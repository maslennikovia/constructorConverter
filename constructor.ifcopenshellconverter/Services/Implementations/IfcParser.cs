using Python.Runtime;
using System.Runtime.InteropServices;

namespace constructor.ifcopenshellconverter.Services.Implementations;

public class IfcParser {
    [DllImport("libifc_wrapper.so")]
    public static extern IntPtr parse_ifc(string filename);

    public void Parse1(string filepath) {
        var result = Marshal.PtrToStringAnsi(parse_ifc(filepath));
        Console.WriteLine(result);
    }
    
    public void Parse(string filepath) {
        using (Py.GIL()) {
            dynamic ifcopenshell = Py.Import("ifcopenshell");
            dynamic file = ifcopenshell.open(filepath);
            // Работа с IFC-файлом
        }
    }
}