//-------------------------------------------------------------------------------
// <copyright file="TransitionDictionary.cs" company="Appccelerate">
//   Copyright (c) 2008-2015
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
//-------------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Appccelerate.StateMachine.Machine.Transitions
{
    /// <summary>
    ///     Manages the transitions of a state.
    /// </summary>
    /// <typeparam name="TState">The type of the state.</typeparam>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    public class TransitionDictionary<TState, TEvent> : ITransitionDictionary<TState, TEvent>
        where TState : IComparable
        where TEvent : IComparable
    {
        /// <summary>
        ///     The state this transition dictionary belongs to.
        /// </summary>
        private readonly IState<TState, TEvent> state;

        /// <summary>
        ///     The transitions.
        /// </summary>
        private readonly Dictionary<TEvent, List<ITransition<TState, TEvent>>> transitions;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransitionDictionary&lt;TState, TEvent&gt;" /> class.
        /// </summary>
        /// <param name="state">The state.</param>
        public TransitionDictionary(IState<TState, TEvent> state)
        {
            this.state = state;
            transitions = new Dictionary<TEvent, List<ITransition<TState, TEvent>>>();
        }

        /// <summary>
        ///     Gets the transitions for the specified event id.
        /// </summary>
        /// <value>transitions for the event id.</value>
        /// <param name="eventId">Id of the event.</param>
        /// <returns>The transitions for the event id.</returns>
        public ICollection<ITransition<TState, TEvent>> this[TEvent eventId]
        {
            get
            {
                List<ITransition<TState, TEvent>> result;

                transitions.TryGetValue(eventId, out result);

                return result;
            }
        }

        /// <summary>
        ///     Adds the specified event id.
        /// </summary>
        /// <param name="eventId">The event id.</param>
        /// <param name="transition">The transition.</param>
        public void Add(TEvent eventId, [NotNull] ITransition<TState, TEvent> transition)
        {
            CheckTransitionDoesNotYetExist(transition);

            transition.Source = state;

            MakeSureEventExistsInTransitionList(eventId);

            transitions[eventId].Add(transition);
        }

        /// <summary>
        ///     Gets all transitions.
        /// </summary>
        /// <returns>All transitions.</returns>
        public IEnumerable<TransitionInfo<TState, TEvent>> GetTransitions()
        {
            var list = new List<TransitionInfo<TState, TEvent>>();
            foreach (var eventId in transitions.Keys)
            {
                GetTransitionsOfEvent(eventId, list);
            }

            return list;
        }

        /// <summary>
        ///     Throws an exception if the specified transition is already defined on this state.
        /// </summary>
        /// <param name="transition">The transition.</param>
        private void CheckTransitionDoesNotYetExist(ITransition<TState, TEvent> transition)
        {
            if (transition.Source != null)
            {
                throw new InvalidOperationException(TransitionsExceptionMessages.TransitionDoesAlreadyExist(transition,
                    state));
            }
        }

        /// <summary>
        ///     If there is no entry in the <see cref="transitions" /> dictionary then one is created.
        /// </summary>
        /// <param name="eventId">The event id.</param>
        private void MakeSureEventExistsInTransitionList(TEvent eventId)
        {
            if (transitions.ContainsKey(eventId))
            {
                return;
            }

            var list = new List<ITransition<TState, TEvent>>();
            transitions.Add(eventId, list);
        }

        /// <summary>
        ///     Gets all the transitions associated to the specified event.
        /// </summary>
        /// <param name="eventId">The event id.</param>
        /// <param name="list">The list to add the transition.</param>
        private void GetTransitionsOfEvent(TEvent eventId, List<TransitionInfo<TState, TEvent>> list)
        {
            foreach (var transition in transitions[eventId])
            {
                list.Add(new TransitionInfo<TState, TEvent>(eventId, transition.Source, transition.Target,
                    transition.Guard, transition.Actions));
            }
        }
    }
}