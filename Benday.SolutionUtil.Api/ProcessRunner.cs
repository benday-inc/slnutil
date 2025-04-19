using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Benday.SolutionUtil.Api;

public class ProcessRunner
{
    private const int TIMEOUT_IN_MILLISECS = 10000;
    private const int EXIT_CODE_SUCCESS = 0;
    private const int EXIT_CODE_NOT_SET = -1;

    public ProcessRunner(ProcessStartInfo startInfo)
    {
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        StartInfo = startInfo;
    }

    public ProcessStartInfo StartInfo { get; private set; }

    private bool _hasRunBeenCalled = false;

    public void Run()
    {
        if (_hasRunBeenCalled == true)
        {
            throw new InvalidOperationException($"Cannot call run a second time.");
        }

        _hasRunBeenCalled = true;

        var timeout = TIMEOUT_IN_MILLISECS;

        using (var process = new Process())
        {
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.StartInfo = StartInfo ??
                throw new InvalidOperationException(
                    "StartInfo was null");

            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        errorBuilder.AppendLine(e.Data);
                    }
                };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var exitCode = EXIT_CODE_NOT_SET;

                if (
                    process.WaitForExit(timeout) &&
                    outputWaitHandle.WaitOne(timeout) &&
                    errorWaitHandle.WaitOne(timeout))
                {
                    // Process completed. Check process.ExitCode here.
                    exitCode = process.ExitCode;
                }
                else
                {
                    SetResultData(true, outputBuilder, errorBuilder);

                    IsTimeout = true;

                    throw new TimeoutException(
                        $"Process timed out after {timeout} milliseconds.");
                }

                if (process.ExitCode != EXIT_CODE_SUCCESS)
                {
                    SetResultData(true, outputBuilder, errorBuilder);
                }
                else
                {
                    SetResultData(false, outputBuilder, errorBuilder);
                }
            }
        }
    }

    private void SetResultData(
        bool isError,
        StringBuilder outputBuilder, StringBuilder errorBuilder)
    {
        if (isError == true)
        {
            IsError = true;
            IsSuccess = false;
        }
        else
        {
            IsError = false;
            IsSuccess = true;
        }

        OutputText = outputBuilder.ToString();
        ErrorText = errorBuilder.ToString();
    }

    public bool IsError { get; private set; }
    public bool IsSuccess { get; private set; }
    public bool IsTimeout { get; private set; }
    public bool HasCompleted { get => IsError | IsSuccess; }
    public string OutputText { get; private set; } = string.Empty;
    public string ErrorText { get; private set; } = string.Empty;
}
