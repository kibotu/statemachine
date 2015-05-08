//-------------------------------------------------------------------------------
// <copyright file="CsvTransitionsWriter.cs" company="Appccelerate">
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
using System.IO;
using System.Linq;
using Appccelerate.StateMachine.Machine;
using Appccelerate.StateMachine.Machine.Transitions;
using JetBrains.Annotations;

namespace Appccelerate.StateMachine.Reports
{
    /// <summary>
    ///     Writes the transitions of a state machine to a stream as csv.
    /// </summary>
    /// <typeparam name="TState">The type of the state.</typeparam>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    public class CsvTransitionsWriter<TState, TEvent>
        where TState : IComparable
        where TEvent : IComparable
    {
        private readonly StreamWriter writer;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CsvTransitionsWriter&lt;TState, TEvent&gt;" /> class.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public CsvTransitionsWriter(StreamWriter writer)
        {
            this.writer = writer;
        }

        /// <summary>
        ///     Writes the transitions of the specified states.
        /// </summary>
        /// <param name="states">The states.</param>
        public void Write([NotNull] IEnumerable<IState<TState, TEvent>> states)
        {
            states = states.ToList();

            WriteTransitionsHeader();

            foreach (var state in states)
            {
                ReportTransitionsOfState(state);
            }
        }

        private void WriteTransitionsHeader()
        {
            writer.WriteLine("Source;Event;Guard;Target;Actions");
        }

        private void ReportTransitionsOfState(IState<TState, TEvent> state)
        {
            foreach (var transition in state.Transitions.GetTransitions())
            {
                ReportTransition(transition);
            }
        }

        private void ReportTransition(TransitionInfo<TState, TEvent> transition)
        {
            var source = transition.Source.ToString();
            var target = transition.Target != null ? transition.Target.Id.ToString() : "internal transition";
            var eventId = transition.EventId.ToString();

            var guard = transition.Guard != null ? transition.Guard.Describe() : string.Empty;
            var actions = string.Join(", ", transition.Actions.Select(action => action.Describe()));

            writer.WriteLine(
                "{0};{1};{2};{3};{4}",
                source,
                eventId,
                guard,
                target,
                actions);
        }
    }
}