using System.Collections.Generic;

namespace  ProiectLFC2
{

public class VariableInfo
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Value { get; set; } 
    public bool IsConst { get; set; }
}

public class FunctionInfo
{
    public string Name { get; set; }
    public string ReturnType { get; set; }
    public List<string> Parameters { get; set; } = new List<string>(); 
    public List<VariableInfo> LocalVariables { get; set; } = new List<VariableInfo>();
    public List<string> ControlStructures { get; set; } = new List<string>(); 
    public bool IsRecursive { get; set; }
    public bool IsMain { get; set; }
}
}