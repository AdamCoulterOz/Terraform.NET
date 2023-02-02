using TF.Attributes;

namespace TF;
public abstract class Credential : ICliAttributed { }
public class VoidCredential : Credential { }