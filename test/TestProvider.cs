namespace TF.Tests.Unit
{
	internal class TestProvider : Provider
	{
		public TestProvider() : base(new VoidCredential())
		{
		}

		protected internal override string Name => "test";
	}
}