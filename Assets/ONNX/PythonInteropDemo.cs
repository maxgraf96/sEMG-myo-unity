using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

public class PythonInteropDemo
{
    private static Process process;

    public PythonInteropDemo()
    {
        process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "C:\\Users\\Max\\PycharmProjects\\sEMG-unity-bridge\\venv\\Scripts\\python.exe", // Use "python3" on some Unix-based systems
                Arguments = "C:\\Users\\Max\\PycharmProjects\\sEMG-unity-bridge\\data_processing.py",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        
        // Make a 100x8 dummy array
        float[] emg1D = new float[800];
        for (int i = 0; i < 800; i++)
        {
            emg1D[i] = i;
        }
    }

    public List<float[]> ProcessEMGData(float[] emg1D)
    {
        var serialised = JsonConvert.SerializeObject(emg1D);

        // Send the command to call the function
        var input = "call_function|" + serialised;
        process.StandardInput.WriteLine(input);

        // Read the response from the Python script
        string output = process.StandardOutput.ReadLine();
        
        // Parse the string
        var list = JsonConvert.DeserializeObject<List<float[]>>(output);

        return list;
    }

    public void Quit()
    {
        // Send the exit command to the Python script
        process.StandardInput.WriteLine("exit");
        // Wait for the Python process to finish and close it
        process.WaitForExit();
        process.Close();
    }
}
