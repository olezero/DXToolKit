#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Bag.cs" company="GAMADU.COM">
//     Copyright � 2013 GAMADU.COM. All rights reserved.
//
//     Redistribution and use in source and binary forms, with or without modification, are
//     permitted provided that the following conditions are met:
//
//        1. Redistributions of source code must retain the above copyright notice, this list of
//           conditions and the following disclaimer.
//
//        2. Redistributions in binary form must reproduce the above copyright notice, this list
//           of conditions and the following disclaimer in the documentation and/or other materials
//           provided with the distribution.
//
//     THIS SOFTWARE IS PROVIDED BY GAMADU.COM 'AS IS' AND ANY EXPRESS OR IMPLIED
//     WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
//     FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL GAMADU.COM OR
//     CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
//     CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
//     SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
//     ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//     NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
//     ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//     The views and conclusions contained in the software and documentation are those of the
//     authors and should not be interpreted as representing official policies, either expressed
//     or implied, of GAMADU.COM.
// </copyright>
// <summary>
//   Class Bag.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#endregion File description

namespace DXToolKit.ECS.Utils {
	#region Using statements

	using global::System;
	using global::System.Collections;
	using global::System.Collections.Generic;

	#endregion Using statements

	/// <summary>Class Bag.</summary>
	/// <typeparam name="T">The <see langword="Type"/> T.</typeparam>
	public class Bag<T> : IEnumerable<T> {
		/// <summary>The elements.</summary>
		private T[] elements;

		/// <summary>Initializes a new instance of the <see cref="Bag{T}"/> class.</summary>
		/// <param name="capacity">The capacity.</param>
		public Bag(int capacity = 16) {
			elements = new T[capacity];
			Count = 0;
		}

		/// <summary>Gets the capacity.</summary>
		/// <value>The capacity.</value>
		public int Capacity {
			get { return elements.Length; }
		}

		/// <summary>Gets a value indicating whether this instance is empty.</summary>
		/// <value><see langword="true" /> if this instance is empty; otherwise, <see langword="false" />.</value>
		public bool IsEmpty {
			get { return Count == 0; }
		}

		/// <summary>Gets the size.</summary>
		/// <value>The size.</value>
		public int Count { get; private set; }

		/// <summary>Returns the element at the specified position in Bag.</summary>
		/// <param name="index">The index.</param>
		/// <returns>The element from the specified position in Bag.</returns>
		public T this[int index] {
			get { return elements[index]; }

			set {
				if (index >= elements.Length) {
					Grow(index * 2);
					Count = index + 1;
				} else if (index >= Count) {
					Count = index + 1;
				}

				elements[index] = value;
			}
		}

		/// <summary>
		/// Adds the specified element to the end of this bag.
		/// If needed also increases the capacity of the bag.
		/// </summary>
		/// <param name="element">The element to be added to this list.</param>
		public void Add(T element) {
			// is size greater than capacity increase capacity
			if (Count == elements.Length) {
				Grow();
			}

			elements[Count] = element;
			++Count;
		}

		/// <summary>Adds a range of elements into this bag.</summary>
		/// <param name="rangeOfElements">The elements to add.</param>
		public void AddRange(Bag<T> rangeOfElements) {
			for (int index = 0, j = rangeOfElements.Count; j > index; ++index) {
				Add(rangeOfElements.Get(index));
			}
		}

		/// <summary>
		/// Removes all of the elements from this bag.
		/// The bag will be empty after this call returns.
		/// </summary>
		public void Clear() {
			// Null all elements so garbage collector can clean up.
			for (int index = Count - 1; index >= 0; --index) {
				elements[index] = default(T);
			}

			Count = 0;
		}

		/// <summary>Determines whether bag contains the specified element.</summary>
		/// <param name="element">The element.</param>
		/// <returns><see langword="true"/> if bag contains the specified element; otherwise, <see langword="false"/>.</returns>
		public bool Contains(T element) {
			for (int index = Count - 1; index >= 0; --index) {
				if (element.Equals(elements[index])) {
					return true;
				}
			}

			return false;
		}

		/// <summary>Gets the specified index.</summary>
		/// <param name="index">The index.</param>
		/// <returns>The specified element.</returns>
		public T Get(int index) {
			return elements[index];
		}

		/// <summary>Removes the specified index.</summary>
		/// <param name="index">The index.</param>
		/// <returns>The removed element.</returns>
		public T Remove(int index) {
			// Make copy of element to remove so it can be returned.
			T result = elements[index];
			--Count;

			// Overwrite item to remove with last element.
			elements[index] = elements[Count];

			// Null last element, so garbage collector can do its work.
			elements[Count] = default(T);
			return result;
		}

		/// <summary>
		/// <para>Removes the first occurrence of the specified element from this Bag, if it is present.</para>
		/// <para>If the Bag does not contain the element, it is unchanged.</para>
		/// <para>Does this by overwriting it was last element then removing last element.</para>
		/// </summary>
		/// <param name="element">The element to be removed from this list, if present.</param>
		/// <returns><see langword="true"/> if this list contained the specified element, otherwise <see langword="false"/>.</returns>
		public bool Remove(T element) {
			for (int index = Count - 1; index >= 0; --index) {
				if (element.Equals(elements[index])) {
					--Count;

					// Overwrite item to remove with last element.
					elements[index] = elements[Count];
					elements[Count] = default(T);

					return true;
				}
			}

			return false;
		}

		/// <summary>Removes all matching elements.</summary>
		/// <param name="bag">The bag.</param>
		/// <returns><see langword="true" /> if found matching elements, <see langword="false" /> otherwise.</returns>
		public bool RemoveAll(Bag<T> bag) {
			bool isResult = false;
			for (int index = bag.Count - 1; index >= 0; --index) {
				if (Remove(bag.Get(index))) {
					isResult = true;
				}
			}

			return isResult;
		}

		/// <summary>Removes the last.</summary>
		/// <returns>The last element.</returns>
		public T RemoveLast() {
			if (Count > 0) {
				--Count;
				T result = elements[Count];

				// default(T) if class = null.
				elements[Count] = default(T);
				return result;
			}

			return default(T);
		}

		/// <summary>Sets the specified index.</summary>
		/// <param name="index">The index.</param>
		/// <param name="element">The element.</param>
		public void Set(int index, T element) {
			if (index >= elements.Length) {
				Grow(index * 2);
				Count = index + 1;
			} else if (index >= Count) {
				Count = index + 1;
			}

			elements[index] = element;
		}

		/// <summary>Returns an enumerator that iterates through a collection.</summary>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerator`1" /> object that can be used to iterate through the collection.</returns>
		IEnumerator<T> IEnumerable<T>.GetEnumerator() {
			return new BagEnumerator<T>(this);
		}

		/// <summary>Returns an enumerator that iterates through a collection.</summary>
		/// <returns>An <see cref="T:System.Collections.Generic.IEnumerator`1" /> object that can be used to iterate through the collection.</returns>
		IEnumerator IEnumerable.GetEnumerator() {
			return new BagEnumerator<T>(this);
		}

		/// <summary>Grows this instance.</summary>
		private void Grow() {
			Grow((int) (elements.Length * 1.5) + 1);
		}

		/// <summary>Grows the specified new capacity.</summary>
		/// <param name="newCapacity">The new capacity.</param>
		private void Grow(int newCapacity) {
			T[] oldElements = elements;
			elements = new T[newCapacity];
			Array.Copy(oldElements, 0, elements, 0, oldElements.Length);
		}
	}
}