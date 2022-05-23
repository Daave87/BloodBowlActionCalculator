﻿using ActionCalculator.Abstractions;
using Action = ActionCalculator.Abstractions.Action;

namespace ActionCalculator
{
    public class CalculationBuilder : ICalculationBuilder
    {
        private readonly IActionBuilderFactory _actionBuilderFactory;
        private readonly IPlayerBuilder _playerBuilder;
        private readonly List<PlayerAction> _playerActions = new();

        public CalculationBuilder(IActionBuilderFactory actionBuilderFactory, IPlayerBuilder playerBuilder)
        {
            _actionBuilderFactory = actionBuilderFactory;
            _playerBuilder = playerBuilder;
        }

        public Calculation Build(string calculation)
        {
            BuildPlayerActions(calculation, new Player(), 0);

            return new Calculation(_playerActions.ToArray());
        }

        private void AddPlayerActions(IEnumerable<PlayerAction> playerActions)
        {
            foreach (var playerAction in playerActions)
            {
                playerAction.Index = _playerActions.Count;
                _playerActions.Add(playerAction);
            }
        }

        private void BuildPlayerActions(string calculation, Player player, int depth)
        {
            var (specialCharacter, index) = GetFirstSpecialCharacter(calculation);

            switch (specialCharacter)
            {
                case ':':
                    player = _playerBuilder.Build(calculation[..index]);
                    BuildPlayerActions(calculation[(index + 1)..], player, depth);
                    break;
                case '{':
                    BuildPlayerActionsWithBranch(calculation, player, depth, index, calculation.LastIndexOf('}'), false);
                    break;
                case '[':
                    BuildPlayerActionsWithBranch(calculation, player, depth, index, calculation.LastIndexOf(']'), true);
                    break;
                case '(':
                    BuildPlayerActionsWithAlternateBranches(calculation, player, depth, index, calculation.LastIndexOf(')'));
                    break;
                case ';':
                    BuildPlayerActionsForMultiplePlayers(calculation, player, depth, index);
                    break;
                default:
                    BuildPlayerActionsForOnePlayer(calculation, player, depth);
                    break;
            }
        }

        private static Tuple<char?, int> GetFirstSpecialCharacter(string calculation)
        {
            for (var i = 0; i < calculation.Length; i++)
            {
                var character = calculation[i];
                foreach (var specialCharacter in new List<char> { ':', ';', '{', '[', '(' }.Where(x => character == x))
                {
                    return new Tuple<char?, int>(specialCharacter, i);
                }
            }

            return new Tuple<char?, int>(null, -1);
        }

        private void BuildPlayerActionsForOnePlayer(string calculation, Player player, int depth) =>
            AddPlayerActions(GetActions(calculation).Select(x => new PlayerAction(player, x, depth)));

        private void BuildPlayerActionsForMultiplePlayers(string calculation, Player player, int depth, int indexOfPlayerEnd)
        {
            AddPlayerActions(GetActions(calculation[..indexOfPlayerEnd]).Select(x => new PlayerAction(player, x, depth)));
            BuildPlayerActions(calculation[(indexOfPlayerEnd + 1)..], new Player(), depth);
        }

        private void BuildPlayerActionsWithBranch(string calculation, Player player, int depth, int indexOfOpeningBracket, int lastIndexOfClosingBracket, bool terminatesCalculation)
        {
            if (lastIndexOfClosingBracket == -1)
            {
                throw new Exception("No matching closing bracket.");
            }

            AddPlayerActions(GetActions(calculation[..indexOfOpeningBracket]).Select(x => new PlayerAction(player, x, depth)));

            var calculationLength = _playerActions.Count;

            BuildPlayerActions(calculation[(indexOfOpeningBracket + 1)..lastIndexOfClosingBracket], player, depth + 1);

            var branchLength = _playerActions.Count - calculationLength;

            if (branchLength > 0)
            {
                _playerActions[calculationLength].RequiresNonCriticalFailure = true;
                _playerActions[calculationLength + branchLength - 1].EndOfBranch = true;

                if (terminatesCalculation)
                {
                    _playerActions[calculationLength + branchLength - 1].TerminatesCalculation = true;
                }
            }

            AddPlayerActions(GetActions(calculation[(lastIndexOfClosingBracket + 1)..]).Select(x => new PlayerAction(player, x, depth)));
        }

        private void BuildPlayerActionsWithAlternateBranches(string calculation, Player player, int depth, int indexOfOpeningBracket, int lastIndexOfClosingBracket)
        {
            if (lastIndexOfClosingBracket == -1)
            {
                throw new Exception("No matching closing bracket.");
            }

            AddPlayerActions(GetActions(calculation[..indexOfOpeningBracket]).Select(x => new PlayerAction(player, x, depth)));

            var branches = calculation[(indexOfOpeningBracket + 1)..lastIndexOfClosingBracket].Split(")(");

            for (var i = 0; i < branches.Length; i++)
            {
                var calculationLength = _playerActions.Count;

                BuildPlayerActions(branches[i], player, depth + 1);

                for (var j = calculationLength; j < _playerActions.Count; j++)
                {
                    _playerActions[j].BranchId = i + 1;
                }
            }

            AddPlayerActions(GetActions(calculation[(lastIndexOfClosingBracket + 1)..]).Select(x => new PlayerAction(player, x, depth)));
        }

        private IEnumerable<Action> GetActions(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                yield break;
            }

            foreach (var actionString in input.Split(','))
            {
                var actionSplit = actionString.Split('|');

                yield return _actionBuilderFactory.GetActionBuilder(actionSplit[0]).Build(actionSplit[0]);

                if (actionSplit.Length == 1)
                {
                    continue;
                }

                var action = _actionBuilderFactory.GetActionBuilder(actionSplit[1]).Build(actionSplit[0]);
                //action.RequiresNonCriticalFailure = true;
                //action.RequiresDauntlessFailure = true;

                yield return action;
            }
        }
    }
}
