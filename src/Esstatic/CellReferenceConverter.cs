namespace Esstatic {
	// rows and columns start from 1 to match with spreadsheets
	public class CellReferenceConverter {
		const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

		public string ToColumnName(int column) {
			var output = "";

			while (column > Letters.Length) {
				output = Letters[(column - 1) % Letters.Length] + output;
				column /= Letters.Length;
			}

			output = Letters[(column - 1) % Letters.Length] + output;

			return output;
		}

		public string ToRelativeCellReference(int column, int row) =>
			$"{ToColumnName(column)}{row}";
	}
}
