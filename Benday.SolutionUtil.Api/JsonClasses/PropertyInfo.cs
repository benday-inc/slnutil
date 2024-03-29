﻿using System;
using System.Linq;

namespace Benday.SolutionUtil.Api.JsonClasses;

public class PropertyInfo
{
    public string JsonName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsArray { get; set; } = false;
}
