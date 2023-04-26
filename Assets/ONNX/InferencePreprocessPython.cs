using System;

namespace ONNX
{
    public class InferencePreprocessPython
    {
        // public static void Initialize()
        // {
        //     string pythonDll = @"C:\Users\Max\.pyenv\pyenv-win\versions\3.8.2\python38.dll";
        //     Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDll);
        //     PythonEngine.Initialize();
        // }
        //
        // public static string RunPythonCodeAndReturn(string pycode, string returnedVariableName)
        // {
        //     object returnedVariable = new object();
        //     Initialize();
        //     using (Py.GIL())
        //     {
        //         using (PyModule scope = Py.CreateScope())
        //         {
        //             scope.Exec(pycode);
        //             returnedVariable = scope.Get<object>(returnedVariableName);
        //         }
        //     }
        //     return returnedVariable.ToString();
        // }
    }
}