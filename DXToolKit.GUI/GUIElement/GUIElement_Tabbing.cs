using System;
using System.Linq;

namespace DXToolKit.GUI {
	public partial class GUIElement {
		private int m_tabOffset = 0;
		private int m_tabIndex = -1;
		private bool m_tabbable = false;

		/// <summary>
		/// Gets or sets a value indicating if this control should stop any tab recurse through the lineage
		/// Useful for a "parent" element that wants to contain all tabbing within itself
		/// Default false
		/// </summary>
		public bool stopTabRecurse = false;

		/// <summary>
		/// Sets the tab index of the element. If 0 or lower this element will not be "tabbable"
		/// </summary>
		public int TabIndex {
			get => m_tabIndex;
			set {
				m_tabIndex = value;
				m_tabbable = value >= 0;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating if this element is tabbable
		/// </summary>
		public bool Tabbable {
			get => m_tabbable;
			set => m_tabbable = value;
		}

		/// <summary>
		/// Implementation in progress
		/// </summary>
		public event Action TabFocus;

		/// <summary>
		/// Implementation in progress
		/// </summary>
		protected virtual void OnTabFocus() => TabFocus?.Invoke();


		/// <summary>
		/// Sets focus to the next neighbour child
		/// </summary>
		/// <param name="ignoreChildren">If child controls should not be checked if they tabbable</param>
		/// <returns>True if new element received focus, false if not</returns>
		public bool TabNext(bool ignoreChildren = false) {
			if (stopTabRecurse) {
				return false;
			}

			// If we have children, should maybe tab into those?
			if (!ignoreChildren && m_childElements.Count > 0) {
				var firstChild = m_childElements.OrderBy(element => element.m_tabIndex).FirstOrDefault(element => element.m_tabbable && element.m_tabIndex >= 0);
				if (firstChild != null) {
					firstChild.Focus();
					firstChild.OnTabFocus();
					return true;
				}
			}

			// Tabbable check should be handled after child element tabbing, since children could still be tabbable
			if (!m_tabbable) {
				return false;
			}

			if (m_parentElement != null) {
				var nextElement = m_parentElement.m_childElements.OrderBy(element => element.m_tabIndex).FirstOrDefault(element => element.m_tabIndex >= m_tabIndex && element != this && element.m_tabbable);
				if (nextElement == null) {
					if (m_parentElement.TabNext(true)) {
						return true;
					}

					// Go back to start? 
					nextElement = m_parentElement.m_childElements.OrderBy(element => element.m_tabIndex).FirstOrDefault(element => element.m_tabbable);

					if (nextElement != null) {
						nextElement.Focus();
						nextElement.OnTabFocus();
						return true;
					}

					return false;
				}

				nextElement.Focus();
				nextElement.OnTabFocus();
				return true;
			}

			return false;
		}


		/// <summary>
		/// Sets focus to the previous neighbour child
		/// </summary>
		/// <param name="onlyChildren">If to only check child elements</param>
		/// <returns>True if new element received focus, false if not</returns>
		public bool TabPrevious(bool onlyChildren = false) {
			if (onlyChildren) {
				var firstChild = m_childElements.OrderBy(element => -element.m_tabIndex).FirstOrDefault(element => element.m_tabbable);
				if (firstChild != null) {
					firstChild.Focus();
					firstChild.OnTabFocus();
					return true;
				}

				return false;
			}

			if (stopTabRecurse) {
				return false;
			}

			if (!m_tabbable) {
				return false;
			}

			if (m_parentElement != null) {
				var prevElement = m_parentElement.m_childElements.OrderBy(element => -element.m_tabIndex).FirstOrDefault(element => element.m_tabIndex <= m_tabIndex && element != this && element.m_tabbable);
				if (prevElement != null) {
					if (prevElement.TabPrevious(true)) {
						return true;
					}

					prevElement.Focus();
					prevElement.OnTabFocus();
					return true;
				}

				// Blocked, back to last
				if (m_parentElement.stopTabRecurse) {
					var lastElement = m_parentElement.m_childElements.OrderBy(element => -element.m_tabIndex).FirstOrDefault(element => element.m_tabbable);
					if (lastElement != null) {
						if (lastElement.TabPrevious(true)) {
							return true;
						}
					}

					return false;
				}

				m_parentElement.Focus();
				m_parentElement.OnTabFocus();
				return true;
			}

			return false;
		}
	}
}