using System;
using System.Collections.Generic;
using System.Linq;

namespace Benday.SolutionUtil.Api.Snippets;

public class StringParsingUtility
{
    public static List<int> GetStartPositions(string line)
    {
        var startOfVar = "${";

        if (line.Contains(startOfVar) == false)
        {
            return new List<int>();
        }

        int indexOf = -1;
        var startPositions = new List<int>();

        do
        {
            var fromIndex = indexOf + 1;

            if (fromIndex >= line.Length)
            {
                indexOf = -1;
            }
            else
            {
                indexOf = line.IndexOf(startOfVar, fromIndex);
            }

            if (indexOf != -1)
            {
                startPositions.Add(indexOf);
            }

        } while (indexOf != -1);

        var startIndex = line.IndexOf(startOfVar);

        return startPositions;
    }

    public static List<int> GetEndPositions(string line)
    {
        var endOfVar = "}";

        if (line.Contains(endOfVar) == false)
        {
            return new List<int>();
        }

        int indexOf = -1;
        var startPositions = new List<int>();

        do
        {
            if (indexOf <= 0)
            {
                indexOf = line.IndexOf(endOfVar);
            }
            else
            {
                var fromIndex = indexOf + 1;

                if (fromIndex >= line.Length)
                {
                    indexOf = -1;
                }
                else
                {
                    indexOf = line.IndexOf(endOfVar, fromIndex);
                }
            }

            if (indexOf != -1)
            {
                startPositions.Add(indexOf);
            }

        } while (indexOf != -1);

        var startIndex = line.IndexOf(endOfVar);

        return startPositions;
    }

    public static bool ContainsNestedTokens(string line)
    {
        var startPositions = GetStartPositions(line);

        if (startPositions.Count < 2)
        {
            return false;
        }
        else
        {
            var endPositions = GetEndPositions(line);

            var tokens = new List<TokenPosition>();

            startPositions.ForEach(x => tokens.Add(new TokenPosition()
            {
                Position = x,
                TokenType = TokenType.Start
            }));

            endPositions.ForEach(x => tokens.Add(new TokenPosition()
            {
                Position = x,
                TokenType = TokenType.End
            }));

            var sortedTokens = tokens.OrderBy(x => x.Position).ToList();

            int tokenCount = 0;

            foreach (var token in sortedTokens)
            {
                if (token.TokenType == TokenType.Start)
                {
                    tokenCount++;
                }
                else
                {
                    tokenCount--;
                }

                if (tokenCount > 1)
                {
                    return true;
                }
            }

            return false;

        }

    }

    public static string GetStringBetweenFirstTokens(string line, List<TokenPosition> positions)
    {
        if (positions.Count < 2)
        {
            throw new InvalidOperationException($"Not enough positions");
        }

        // position without the start of token chars
        int start0 = positions[0].Position + 2;
        int start1 = positions[1].Position;

        return line.Substring(start0, start1 - start0);
    }

    public static List<TokenPosition> GetPositions(string line)
    {
        var startPositions = GetStartPositions(line);

        if (startPositions.Count < 2)
        {
            throw new InvalidOperationException($"Line is does not have nested tokens");
        }
        else
        {
            var endPositions = GetEndPositions(line);

            var tokens = new List<TokenPosition>();

            startPositions.ForEach(x => tokens.Add(new TokenPosition()
            {
                Position = x,
                TokenType = TokenType.Start
            }));

            endPositions.ForEach(x => tokens.Add(new TokenPosition()
            {
                Position = x,
                TokenType = TokenType.End
            }));

            var sortedTokens = tokens.OrderBy(x => x.Position).ToList();

            return sortedTokens;
        }
    }
    public static string GetValueBeforeSlash(string input)
    {
        var tokens = input.Split('/');

        return tokens[0];
    }
}
