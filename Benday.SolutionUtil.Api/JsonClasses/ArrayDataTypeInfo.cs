using System.Text.Json;

namespace Benday.SolutionUtil.Api.JsonClasses;

public class ArrayDataTypeInfo
    {
        public bool IsScalar { get; set; }
        public JsonValueKind Kind { get; set; }
        public string ProposedDataType { get; set; } = string.Empty;
        public bool IsEmpty { get; set; }
    }
