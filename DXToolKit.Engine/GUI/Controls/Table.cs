using System;
using System.Linq;
using DXToolKit;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	/// <summary>
	/// Table view gui element
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class Table<T> : GUIElement {
		#region Class definitions

		/// <inheritdoc />
		private class HeaderLabel : Button {
			public readonly int ColumnIndex;

			public HeaderLabel(string text, int columnIndex) : base(text) {
				ColumnIndex = columnIndex;
				TextAlignment = TextAlignment.Leading;
				TextOffset = new Vector2(4, 0);
			}
		}

		/// <summary>
		/// Table row class used for each row in the table
		/// </summary>
		public sealed class Tablerow : ActiveElement {
			private class RowLabel : Label {
				public RowLabel(string text) : base(text) { }
			}

			/// <summary>
			/// Labels used to render text
			/// </summary>
			private RowLabel[] m_labels;

			/// <summary>
			/// Gets the column data (The strings used for display)
			/// </summary>
			public readonly string[] ColumnData;

			/// <summary>
			/// Gets a reference to the stored user data
			/// </summary>
			public readonly T UserData;

			internal Tablerow(string[] columnData, T userData) {
				ColumnData = columnData;
				UserData = userData;
				m_labels = new RowLabel[columnData.Length];
				for (int i = 0; i < m_labels.Length; i++) {
					m_labels[i] = Append(new RowLabel(columnData[i]));
				}
			}

			internal void SetDataSize(int columnIndex, float left, float width) {
				m_labels[columnIndex].Left = left;
				m_labels[columnIndex].Width = width;
				m_labels[columnIndex].Height = Height;
				m_labels[columnIndex].Top = 0;
			}
		}

		#endregion

		private TableColumnDefinition[] m_columns;
		private GUIElement[] m_headers;
		private Listbox<Tablerow> m_tableData;
		private float m_headerHeight = 24.0F;
		private float m_rowHeight = 24.0F;
		private ArrowIcon m_sortIcon;
		private int m_currentOrderBy = -1;
		private bool m_reverseOrder = false;

		/// <summary>
		/// Gets or sets a value indicating if a sorting should be reset after the third click on a column header
		/// Normal is Asc-Desc on each click
		/// With this it runs Asc-Desc-NoSort
		/// </summary>
		public bool ResetSortingOnThirdClick = true;

		/// <summary>
		/// Event handler for when a row is clicked
		/// </summary>
		/// <param name="userData">The user data connected to the row</param>
		/// <param name="rowColumnData">The row column strings of the row</param>
		public delegate void RowEventHandler(T userData, string[] rowColumnData);

		/// <summary>
		/// Event invoked when a row is clicked
		/// </summary>
		public event RowEventHandler RowClicked;

		/// <summary>
		/// Invoked when a row is clicked
		/// </summary>
		/// <param name="userData">The user data connected to the row</param>
		/// <param name="rowColumnData">The row column strings of the row</param>
		protected virtual void OnRowClicked(T userData, string[] rowColumnData) => RowClicked?.Invoke(userData, rowColumnData);

		/// <summary>
		/// Gets a reference to the column definitions of the table
		/// </summary>
		public TableColumnDefinition[] Columns => m_columns;

		/// <summary>
		/// Gets a reference to the headers of the table
		/// </summary>
		public GUIElement[] Headers => m_headers;

		/// <summary>
		/// Gets a reference to the underlying listbox used to render the rows
		/// </summary>
		public Listbox<Tablerow> TableData => m_tableData;

		/// <summary>
		/// Gets or sets a value indicating the height of the header of the table
		/// </summary>
		public float HeaderHeight {
			get => m_headerHeight;
			set {
				m_headerHeight = value;
				Organize();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating the row height of the table
		/// </summary>
		public float RowHeight {
			get => m_rowHeight;
			set {
				m_rowHeight = value;
				Organize();
			}
		}

		/// <summary>
		/// Creates a new empty table based on input column definitions
		/// </summary>
		/// <param name="columns">The column definitions</param>
		public Table(TableColumnDefinition[] columns) {
			m_columns = columns;
			m_headers = new GUIElement[columns.Length];
			for (int i = 0; i < m_headers.Length; i++) {
				var header = Append(new HeaderLabel(columns[i].HeaderText, i));
				header.Click += args => {
					OrderBy(header.ColumnIndex);
				};
				m_headers[i] = header;
			}

			m_tableData = Append(new Listbox<Tablerow> {
				UseDynamicHeight = false,
				ShineOpacity = 0.0F,
			});
			m_sortIcon = Append(new ArrowIcon(0, ArrowType.FilledTriangle));
			m_sortIcon.Width = m_sortIcon.Height = m_headerHeight / 2.0F;
			m_sortIcon.Visible = false;
			m_sortIcon.MoveToFront();


			m_tableData.OptionClicked += option => {
				OnRowClicked(option.UserData, option.ColumnData);
			};

			Width = 12 * 24;
			Height = 12 * 12;
			Organize();
		}

		/// <summary>
		/// Adds a new row to the table
		/// </summary>
		/// <param name="columnData">The per column data</param>
		/// <param name="userData">Optional user data to connect to the row</param>
		/// <exception cref="Exception">Throws an exception if input columnData array count does not match stored column definition array count</exception>
		public void AddRow(string[] columnData, T userData = default) {
			if (columnData.Length != m_columns.Length) {
				throw new Exception($"Input row data count ({columnData.Length}) does not match table column definition count of {m_columns.Length}");
			}

			var row = m_tableData.AddOption(new Tablerow(columnData, userData) {
				Height = m_rowHeight
			});
			PositionRowData(row);
		}

		/// <inheritdoc />
		protected override void OnBoundsChangedDirect() {
			Organize();
			base.OnBoundsChangedDirect();
		}

		private void OrderBy(int columnIndex) {
			// Detect if reverse or not
			if (columnIndex == m_currentOrderBy) {
				// Reset handler when the header is clicked a third time
				// So it goes sort asc, desc then nothing
				if (m_reverseOrder && ResetSortingOnThirdClick) {
					m_sortIcon.Visible = false;
					m_tableData.SetEnumerator(null);
					m_reverseOrder = false;
					m_currentOrderBy = -1;
					return;
				}

				m_reverseOrder = !m_reverseOrder;
			} else {
				m_reverseOrder = false;
			}

			// Update current order by 
			m_currentOrderBy = columnIndex;

			// Detect if sorting by number
			var isNumber = m_columns[columnIndex].SortType == TableColumnSortType.NUMBER;
			m_tableData.SetEnumerator(list => {
				return isNumber ? list.OrderBy(tablerow => m_reverseOrder ? -float.Parse(tablerow.ColumnData[columnIndex]) : float.Parse(tablerow.ColumnData[columnIndex])) :
					m_reverseOrder ? list.OrderByDescending(tablerow => tablerow.ColumnData[columnIndex]) : list.OrderBy(tablerow => tablerow.ColumnData[columnIndex]);
			});

			// Position and scale sort arrow icon
			var header = m_headers[columnIndex];
			m_sortIcon.Right = header.Right - 2.0F;
			m_sortIcon.CenterY = header.Height / 2.0F;
			m_sortIcon.Visible = true;

			if (m_reverseOrder) {
				m_sortIcon.Rotation = 0;
			} else {
				m_sortIcon.Rotation = Mathf.Pi;
			}
		}

		private void PositionRowData(Tablerow row) {
			var xOffset = 0.0F;
			var columnWidth = Width / 12.0F;
			for (int j = 0; j < m_columns.Length; j++) {
				var column = m_columns[j];
				row.SetDataSize(j, xOffset, column.ColumnSpan * columnWidth);
				xOffset += column.ColumnSpan * columnWidth;
			}
		}

		private void Organize() {
			var columnWidth = Width / 12.0F;

			// Setup headers
			if (m_headers != null) {
				var xOffset = 0.0F;
				for (int i = 0; i < m_headers.Length; i++) {
					var columnSpan = m_columns[i].ColumnSpan;
					m_headers[i].X = xOffset;
					m_headers[i].Y = 0.0F;
					m_headers[i].Height = m_headerHeight;
					m_headers[i].Width = columnSpan * columnWidth;

					xOffset += m_headers[i].Width;
				}
			}

			// Setup table data
			if (m_tableData != null) {
				m_tableData.Width = Width;
				m_tableData.Height = Height - m_headerHeight;
				m_tableData.X = 0;
				m_tableData.Y = m_headerHeight;
				for (int i = 0; i < m_tableData.Options.Count; i++) {
					PositionRowData(m_tableData.Options[i]);
					m_tableData.Options[i].Height = m_rowHeight;
				}
			}
		}
	}
}