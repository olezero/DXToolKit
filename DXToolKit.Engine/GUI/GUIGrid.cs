using System;
using System.Collections.Generic;
using System.Linq;

namespace DXToolKit.Engine {
	public class GUIGrid : IGUIGriddable {
		private class GridColumn {
			public int ColumnSpan;
			public IGUIGriddable Element;
		}

		private const float COLUMN_COUNT = 12.0F;

		// Full grid, a list of rows containing a list of columns with corresponding column spans
		private List<List<GridColumn>> m_grid;
		private List<GridColumn> m_currentrow;

		private Action<GUIGrid> m_oncreate;
		private GUIElement m_baseElement;

		private GUIGrid() {
			m_grid = new List<List<GridColumn>>();
			m_currentrow = new List<GridColumn>();
		}

		public float X { get; set; }
		public float Y { get; set; }
		public float Width { get; set; }
		public float Height { get; set; }

		public T Column<T>(T element, int columspan) where T : IGUIGriddable {
			// if a rows total column span + input column span is greater then 12 (default number of columns) add a new row
			if (CurrentRowWidth() + columspan > COLUMN_COUNT) {
				AddRow();
			}

			// Something like this
			m_currentrow.Add(new GridColumn() {
				ColumnSpan = columspan,
				Element = element
			});

			return element;
		}

		private void AddRow() {
			// Add current working row to grid
			m_grid.Add(m_currentrow);

			// Create new row
			m_currentrow = new List<GridColumn>();
		}

		private int CurrentRowWidth() {
			return m_currentrow.Sum(column => column.ColumnSpan);
		}

		public void SubGrid(Action<GUIGrid> oncreate, int columnspan) {
			// Create new grid and store the on create function for later
			var subGrid = new GUIGrid() {
				m_oncreate = oncreate,
				m_baseElement = this.m_baseElement,
			};
			// Add grid to parent
			Column(subGrid, columnspan);
		}


		public static GUIGrid Create(GUIElement baseElement, Action<GUIGrid> oncreate) {
			// Create new grid and store the on create function for later
			var grid = new GUIGrid {
				// Store base element so we can append to it, and get its width and height
				m_baseElement = baseElement,
				// Store on create so we can call it when applying the grid layout
				m_oncreate = oncreate,
				// Store size values of base element
				Width = baseElement.Width,
				Height = baseElement.Height,
				// Position defaults to 0 since this is the base grid, and we are working with local positioning
				X = 0,
				Y = 0,
			};

			// Organize all elements
			grid.OrganizeElements();
			return grid;
		}

		public void RunOrganize(bool updateSize = true) {
			if (updateSize) {
				Width = m_baseElement.Width;
				Height = m_baseElement.Height;
			}

			// Should only position all elements, not append etc
			// Each row height is number of rows divided by height.
			var rowHeight = Height / m_grid.Count;
			var spanWidth = Width / COLUMN_COUNT;
			var yOffset = Y;
			var xOffset = X;

			foreach (var gridrow in m_grid) {
				foreach (var column in gridrow) {
					var columnSpan = column.ColumnSpan;
					var el = column.Element;

					// Set width to column span
					el.Width = spanWidth * columnSpan;
					el.X = xOffset;

					el.Height = rowHeight;
					el.Y = yOffset;

					// offset x
					xOffset += el.Width;

					if (column.Element is GUIGrid subgrid) {
						subgrid.RunOrganize(false);
					}
				}

				yOffset += rowHeight;
				xOffset = X;
			}
		}

		private void OrganizeElements() {
			// Gather all elements to be added to the grid
			m_oncreate?.Invoke(this);

			// if current row has any elements add it
			if (m_currentrow.Count > 0) {
				AddRow();
			}

			foreach (var gridrow in m_grid) {
				foreach (var column in gridrow) {
					if (column.Element is GUIGrid subgrid) {
						subgrid.Width = this.Width;
						subgrid.Height = this.Height;
						subgrid.OrganizeElements();
					}

					if (column.Element is GUIElement element) {
						m_baseElement.Append(element);
					}
				}
			}

			RunOrganize();
		}
	}
}