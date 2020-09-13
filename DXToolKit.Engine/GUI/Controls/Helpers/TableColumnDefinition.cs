namespace DXToolKit.Engine {
	/// <summary>
	/// A structure describing information in a table column
	/// </summary>
	public struct TableColumnDefinition {
		private readonly string m_headerText;
		private readonly int m_columnSpan;

		/// <summary>
		/// Gets the stored header text
		/// </summary>
		public string HeaderText => m_headerText;

		/// <summary>
		/// Gets the stored column span
		/// </summary>
		public int ColumnSpan => m_columnSpan;

		/// <summary>
		/// Gets the stored sorting type
		/// </summary>
		public readonly TableColumnSortType SortType;

		/// <summary>
		/// Creates a new column definition
		/// </summary>
		/// <param name="headerText">The text on the header of the column</param>
		/// <param name="columnSpan">The width (in 1/12) of the column</param>
		/// <param name="sortType">Sort type when user clicks on a header</param>
		public TableColumnDefinition(string headerText, int columnSpan, TableColumnSortType sortType) {
			m_headerText = headerText;
			m_columnSpan = columnSpan;
			SortType = sortType;
		}
	}
}