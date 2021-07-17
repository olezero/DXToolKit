using System;
using System.Collections.Generic;
using System.Linq;

namespace DXToolKit.Engine {
	/// <summary>
	/// Grid system used to position IGUIGriddable
	/// Uses 12 columns
	/// </summary>
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
		private GUIPadding m_gridPadding = new GUIPadding();
		private GUIPadding m_elementPadding = new GUIPadding();

		private GUIGrid() {
			m_grid = new List<List<GridColumn>>();
			m_currentrow = new List<GridColumn>();
		}


		/// <inheritdoc />
		public float X { get; set; }

		/// <inheritdoc />
		public float Y { get; set; }

		/// <inheritdoc />
		public float Width { get; set; }

		/// <inheritdoc />
		public float Height { get; set; }

		/// <summary>
		/// Gets the total row count of the grid
		/// </summary>
		public int RowCount => m_grid.Count;

		/// <summary>
		/// Gets or sets a value indicating if rows should be sized to parent element height divided by row count
		/// If disabled, each row will be sized to the largest child element height
		/// Usefull when generating a grid on a element which has an unknown total height, but child elements should be stacked on top of each other
		/// Default: true
		/// </summary>
		public bool DynamicRowHeight { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating if child elements should automatically be resized to fit the row height.
		/// Depending on if DynamicRowHeight is set, child elements will be set to the largest element in the row, or to the rows height
		/// Default: true
		/// </summary>
		public bool AutoResizeElementHeigth { get; set; } = true;


		/// <summary>
		/// Gets or sets a value indicating if GUI grid is allowed to resize parent so that all elements will be positioned within a whole number.
		/// This does require all padding to be set to a whole number aswell
		/// </summary>
		public bool AllowParentResize = true;

		/// <summary>
		/// Adds a new column.
		/// If a row consists of more then 12 column spans, a new row will be added
		/// </summary>
		/// <param name="element">The IGUIGriddable element to add to the column</param>
		/// <param name="columspan">The column span the griddable should occupy</param>
		/// <typeparam name="T">Type of the element for return value</typeparam>
		/// <returns>The element</returns>
		public T Column<T>(T element, int columspan) where T : IGUIGriddable {
			// if a rows total column span + input column span is greater then 12 (default number of columns) add a new row
			if (CurrentRowWidth() + columspan > COLUMN_COUNT) {
				AddRow();
			}

			// Something like this
			m_currentrow.Add(new GridColumn {
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

		/// <summary>
		/// Adds a sub grid as a element to the grid
		/// </summary>
		/// <param name="oncreate">Method invoked when creating the grid. This will only run once</param>
		/// <param name="columnspan">The span the sub grid should occupy</param>
		public void SubGrid(Action<GUIGrid> oncreate, int columnspan) {
			// Create new grid and store the on create function for later
			var subGrid = new GUIGrid {
				m_oncreate = oncreate,
				m_baseElement = this.m_baseElement,
				// Kinda have to do this or else things might get weird
				AllowParentResize = false,
			};
			// Add grid to parent
			Column(subGrid, columnspan);
		}

		/// <summary>
		/// Creates a new GUI Grid
		/// </summary>
		/// <param name="baseElement">The GUI Element the grid should base its width and height on, aswell as append new elements to</param>
		/// <param name="oncreate">Method invoked when creating the grid. This will only run once</param>
		/// <returns>The newly created grid</returns>
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
				Y = 0
			};

			// Organize all elements
			grid.OrganizeElements();

			return grid;
		}

		/// <summary>
		/// Adds padding to the grid as a whole
		/// </summary>
		/// <param name="padding"></param>
		public void SetGridPadding(GUIPadding padding) {
			m_gridPadding = padding;
		}

		/// <summary>
		/// Sets the padding between elements in the grid
		/// </summary>
		/// <param name="padding"></param>
		public void SetElementPadding(GUIPadding padding) {
			m_elementPadding = padding;
		}

		/// <summary>
		/// Organizes all elements that are part of the grid based on column size etc
		/// </summary>
		/// <param name="updateSize">If the grid should grab size from parent element</param>
		public void RunOrganize(bool updateSize = true) {
			if (updateSize) {
				Width = m_baseElement.Width;
				Height = m_baseElement.Height;
			}

			var targetX = X + m_gridPadding.Left;
			var targetY = Y + m_gridPadding.Top;
			var targetWidth = Width - (m_gridPadding.Left + m_gridPadding.Right);
			var targetHeight = Height - (m_gridPadding.Top + m_gridPadding.Bottom);


			// Should only position all elements, not append etc
			// Each row height is number of rows divided by height.
			var rowHeight = targetHeight / m_grid.Count;
			var spanWidth = targetWidth / COLUMN_COUNT;

			// Set row height to 0 if dynamic height is disabled, this will make each row the same height as the tallest sub element
			if (DynamicRowHeight == false) rowHeight = 0;
			if (AllowParentResize) {
				rowHeight = (float) Math.Ceiling(rowHeight);
				spanWidth = (float) Math.Ceiling(spanWidth);
			}


			var yOffset = targetY;
			var xOffset = targetX;

			var newWidth = 0.0F;
			var newHeight = 0.0F;

			foreach (var gridrow in m_grid) {
				if (DynamicRowHeight == false) {
					// Scan for the highest child element, and set row height to that
					foreach (var column in gridrow) {
						rowHeight = Mathf.Max(column.Element.Height, rowHeight);
					}
				}

				foreach (var column in gridrow) {
					var columnSpan = column.ColumnSpan;
					var el = column.Element;

					// Set width to column span
					el.Width = spanWidth * columnSpan;
					el.X = xOffset;

					// Resize element height if requested
					if (AutoResizeElementHeigth) {
						el.Height = rowHeight;
					}

					el.Y = yOffset;

					if (xOffset + el.Width > newWidth) {
						newWidth = xOffset + el.Width;
					}

					if (yOffset + el.Height > newHeight) {
						newHeight = yOffset + el.Height;
					}

					// offset x
					xOffset += el.Width;

					if (el is GUIGrid subgrid) {
						subgrid.RunOrganize(false);
					} else {
						// Apply element spacing
						el.X += m_elementPadding.Left;
						el.Y += m_elementPadding.Top;
						el.Width -= m_elementPadding.Left + m_elementPadding.Right;
						el.Height -= m_elementPadding.Top + m_elementPadding.Bottom;
					}
				}

				yOffset += rowHeight;
				xOffset = targetX;
			}

			if (!AllowParentResize) return;
			m_baseElement.Height = newHeight + m_gridPadding.Bottom;
			m_baseElement.Width = newWidth + m_gridPadding.Right;
		}

		private void OrganizeElements() {
			// Gather all elements to be added to the grid
			m_oncreate?.Invoke(this);

			// if current row has any elements add it
			if (m_currentrow.Count > 0) AddRow();

			foreach (var gridrow in m_grid) {
				foreach (var column in gridrow) {
					if (column.Element is GUIGrid subgrid) {
						subgrid.Width = Width;
						subgrid.Height = Height;
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