namespace constructor.converter.step.Extensions;

public class XbimExtension
{
    public static string GenerateIfcGlobalId()
    {
        // IFC GlobalId - это base64-кодированный GUID без последних 2 символов '=='
        Guid guid = Guid.NewGuid();
        string base64 = Convert.ToBase64String(guid.ToByteArray());
    
        // IFC GlobalId имеет длину 22 символа
        // Заменяем '/' и '+' на допустимые символы в IFC
        string globalId = base64.Replace("/", "_").Replace("+", "$");
    
        // Обрезаем до 22 символов (стандартная длина в IFC)
        return globalId.Substring(0, 22);
    }
}