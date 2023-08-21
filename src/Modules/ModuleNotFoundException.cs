namespace TF;

public class ModuleNotFoundException(ModuleReference moduleReference)
	: Exception($"Module not found: {moduleReference.Path}")
{
    public ModuleReference ModuleReference { get; set; } = moduleReference;
}
