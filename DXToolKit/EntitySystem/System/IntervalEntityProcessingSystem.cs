#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalEntityProcessingSystem.cs" company="GAMADU.COM">
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
//   Class IntervalEntityProcessingSystem.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#endregion File description

namespace DXToolKit.ECS.System {
	#region Using statements

	using global::System;
	using global::System.Collections.Generic;

	#endregion Using statements

	/// <summary>Class IntervalEntityProcessingSystem.</summary>
	public abstract class IntervalEntityProcessingSystem : IntervalEntitySystem {
		/// <summary>Initializes a new instance of the <see cref="IntervalEntityProcessingSystem"/> class.</summary>
		/// <param name="timeSpan">The time span.</param>
		/// <param name="requiredType">Type of the required.</param>
		/// <param name="otherTypes">The other types.</param>
		protected IntervalEntityProcessingSystem(TimeSpan timeSpan, Type requiredType, params Type[] otherTypes)
			: base(timeSpan, GetMergedTypes(requiredType, otherTypes)) { }

		/// <summary>Initializes a new instance of the <see cref="IntervalEntityProcessingSystem" /> class.</summary>
		/// <param name="timeSpan">The time span.</param>
		/// <param name="aspect">The aspect.</param>
		protected IntervalEntityProcessingSystem(TimeSpan timeSpan, Aspect aspect)
			: base(timeSpan, aspect) { }

		/// <summary>Processes the specified entity.</summary>
		/// <param name="entity">The entity.</param>
		public abstract void Process(Entity entity);

		/// <summary>Processes the entities.</summary>
		/// <param name="entities">The entities.</param>
		protected override void ProcessEntities(IDictionary<int, Entity> entities) {
			foreach (Entity entity in entities.Values) {
				Process(entity);
			}
		}
	}
}