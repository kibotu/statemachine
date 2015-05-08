//-------------------------------------------------------------------------------
// <copyright file="Transition.cs" company="Appccelerate">
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
using System.Globalization;
using Appccelerate.StateMachine.Machine.ActionHolders;
using Appccelerate.StateMachine.Machine.GuardHolders;
using JetBrains.Annotations;

namespace Appccelerate.StateMachine.Machine.Transitions
{
    public class Transition<TState, TEvent>
        : ITransition<TState, TEvent>
        where TState : IComparable
        where TEvent : IComparable
    {
        private readonly List<IActionHolder> actions;
        private readonly IExtensionHost<TState, TEvent> extensionHost;
        private readonly IStateMachineInformation<TState, TEvent> stateMachineInformation;

        public Transition(IStateMachineInformation<TState, TEvent> stateMachineInformation,
            IExtensionHost<TState, TEvent> extensionHost)
        {
            this.stateMachineInformation = stateMachineInformation;
            this.extensionHost = extensionHost;

            actions = new List<IActionHolder>();
        }

        private bool IsInternalTransition
        {
            get { return Target == null; }
        }

        public IState<TState, TEvent> Source { get; set; }
        public IState<TState, TEvent> Target { get; set; }
        public IGuardHolder Guard { get; set; }

        public ICollection<IActionHolder> Actions
        {
            get { return actions; }
        }

        public ITransitionResult<TState, TEvent> Fire([NotNull] ITransitionContext<TState, TEvent> context)
        {
            if (!ShouldFire(context))
            {
                extensionHost.ForEach(extension => extension.SkippedTransition(
                    stateMachineInformation,
                    this,
                    context));

                return TransitionResult<TState, TEvent>.NotFired;
            }

            context.OnTransitionBegin();

            extensionHost.ForEach(extension => extension.ExecutingTransition(
                stateMachineInformation,
                this,
                context));

            var newState = context.State;

            if (!IsInternalTransition)
            {
                UnwindSubStates(context);

                Fire(Source, Target, context);

                newState = Target.EnterByHistory(context);
            }
            else
            {
                PerformActions(context);
            }

            extensionHost.ForEach(extension => extension.ExecutedTransition(
                stateMachineInformation,
                this,
                context));

            return new TransitionResult<TState, TEvent>(true, newState);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Transition from state {0} to state {1}.", Source, Target);
        }

        private static void HandleException(Exception exception, ITransitionContext<TState, TEvent> context)
        {
            context.OnExceptionThrown(exception);
        }

        /// <summary>
        ///     Recursively traverses the state hierarchy, exiting states along
        ///     the way, performing the action, and entering states to the target.
        /// </summary>
        /// <remarks>
        ///     There exist the following transition scenarios:
        ///     0. there is no target state (internal transition)
        ///     --> handled outside this method.
        ///     1. The source and target state are the same (self transition)
        ///     --> perform the transition directly:
        ///     Exit source state, perform transition actions and enter target state
        ///     2. The target state is a direct or indirect sub-state of the source state
        ///     --> perform the transition actions, then traverse the hierarchy
        ///     from the source state down to the target state,
        ///     entering each state along the way.
        ///     No state is exited.
        ///     3. The source state is a sub-state of the target state
        ///     --> traverse the hierarchy from the source up to the target,
        ///     exiting each state along the way.
        ///     Then perform transition actions.
        ///     Finally enter the target state.
        ///     4. The source and target state share the same super-state
        ///     5. All other scenarios:
        ///     a. The source and target states reside at the same level in the hierarchy
        ///     but do not share the same direct super-state
        ///     --> exit the source state, move up the hierarchy on both sides and enter the target state
        ///     b. The source state is lower in the hierarchy than the target state
        ///     --> exit the source state and move up the hierarchy on the source state side
        ///     c. The target state is lower in the hierarchy than the source state
        ///     --> move up the hierarchy on the target state side, afterward enter target state
        /// </remarks>
        /// <param name="source">The source state.</param>
        /// <param name="target">The target state.</param>
        /// <param name="context">The event context.</param>
        private void Fire(IState<TState, TEvent> source, IState<TState, TEvent> target,
            ITransitionContext<TState, TEvent> context)
        {
            if (source == Target)
            {
                // Handles 1.
                // Handles 3. after traversing from the source to the target.
                source.Exit(context);
                PerformActions(context);
                Target.Entry(context);
            }
            else if (source == target)
            {
                // Handles 2. after traversing from the target to the source.
                PerformActions(context);
            }
            else if (source.SuperState == target.SuperState)
            {
                //// Handles 4.
                //// Handles 5a. after traversing the hierarchy until a common ancestor if found.
                source.Exit(context);
                PerformActions(context);
                target.Entry(context);
            }
            else
            {
                // traverses the hierarchy until one of the above scenarios is met.

                // Handles 3.
                // Handles 5b.
                if (source.Level > target.Level)
                {
                    source.Exit(context);
                    Fire(source.SuperState, target, context);
                }
                else if (source.Level < target.Level)
                {
                    // Handles 2.
                    // Handles 5c.
                    Fire(source, target.SuperState, context);
                    target.Entry(context);
                }
                else
                {
                    // Handles 5a.
                    source.Exit(context);
                    Fire(source.SuperState, target.SuperState, context);
                    target.Entry(context);
                }
            }
        }

        private bool ShouldFire(ITransitionContext<TState, TEvent> context)
        {
            try
            {
                return Guard == null || Guard.Execute(context.EventArgument);
            }
            catch (Exception exception)
            {
                extensionHost.ForEach(
                    extention => extention.HandlingGuardException(stateMachineInformation, this, context, ref exception));

                HandleException(exception, context);

                extensionHost.ForEach(
                    extention => extention.HandledGuardException(stateMachineInformation, this, context, exception));

                return false;
            }
        }

        private void PerformActions(ITransitionContext<TState, TEvent> context)
        {
            foreach (var action in actions)
            {
                try
                {
                    action.Execute(context.EventArgument);
                }
                catch (Exception exception)
                {
                    extensionHost.ForEach(
                        extension =>
                            extension.HandlingTransitionException(stateMachineInformation, this, context, ref exception));

                    HandleException(exception, context);

                    extensionHost.ForEach(
                        extension =>
                            extension.HandledTransitionException(stateMachineInformation, this, context, exception));
                }
            }
        }

        private void UnwindSubStates(ITransitionContext<TState, TEvent> context)
        {
            for (var o = context.State; o != Source; o = o.SuperState)
            {
                o.Exit(context);
            }
        }
    }
}