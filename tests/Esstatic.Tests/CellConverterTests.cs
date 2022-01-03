using Xunit;

namespace Esstatic.Tests {
	public class CellConverterTests {
		[Fact]
		public void ToColumnName() {
			var c = new CellReferenceConverter();

			Assert.Equal("A",  c.ToColumnName(1));
			Assert.Equal("B",  c.ToColumnName(2));
			Assert.Equal("C", c.ToColumnName(3));
			Assert.Equal("Z",  c.ToColumnName(26));
			Assert.Equal("AA", c.ToColumnName(27));
			Assert.Equal("AB", c.ToColumnName(28));
			Assert.Equal("AC", c.ToColumnName(29));
		}

		[Fact]
		public void ToRelativeCellReference() {
			var c = new CellReferenceConverter();

			Assert.Equal("A5", c.ToRelativeCellReference(1, 5));
		}
	}
}
