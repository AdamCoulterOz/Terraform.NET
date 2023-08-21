namespace TF.Tests.Unit;

public class TestProvider : Provider<VoidCredential>
{
	public TestProvider() : base(new()) { }
	public override string Name => "test";
}
