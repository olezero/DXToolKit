#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BagEnumerator.cs" company="GAMADU.COM">
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
//   Class BagEnumerator.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#endregion File description

namespace DXToolKit.ECS.Utils {
	#region Using statements

	using global::System.Collections;
	using global::System.Collections.Generic;

	#endregion Using statements

	/// <summary>Class BagEnumerator.</summary>
	/// <typeparam name="T">The <see langword="Type"/> T.</typeparam>
	internal class BagEnumerator<T> : IEnumerator<T> {
		/// <summary>The bag.</summary>
		private volatile Bag<T> bag;

		/// <summary>The index.</summary>
		private volatile int index;

		/// <summary>Initializes a new instance of the <see cref="BagEnumerator{T}"/> class.</summary>
		/// <param name="bag">The bag.</param>
		public BagEnumerator(Bag<T> bag) {
			this.bag = bag;
			Reset();
		}

		/// <summary>Gets the current element in the collection.</summary>
		/// <value>The current element.</value>
		/// <returns>The current element in the collection.</returns>
		T IEnumerator<T>.Current {
			get { return bag.Get(index); }
		}

		/// <summary>Gets the current element in the collection.</summary>
		/// <value>The current.</value>
		/// <returns>The current element in the collection.</returns>
		object IEnumerator.Current {
			get { return bag.Get(index); }
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose() {
			bag = null;
		}

		/// <summary>Advances the enumerator to the next element of the collection.</summary>
		/// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
		public bool MoveNext() {
			return ++index < bag.Count;
		}

		/// <summary>Sets the enumerator to its initial position, which is before the first element in the collection.</summary>
		public void Reset() {
			index = -1;
		}
	}
}