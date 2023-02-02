namespace TF;

public class ModuleNotFoundException : Exception
{
	public ModuleReference ModuleReference { get; set; }

	public ModuleNotFoundException(ModuleReference moduleReference)
		: base($"Module not found: {moduleReference.Path}")
		=> ModuleReference = moduleReference;
}
