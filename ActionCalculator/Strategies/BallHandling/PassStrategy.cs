﻿using ActionCalculator.Abstractions;
using ActionCalculator.Abstractions.Actions;
using ActionCalculator.Abstractions.Calculators;

namespace ActionCalculator.Strategies.BallHandling
{
    public class PassStrategy : IActionStrategy
    {
        private readonly IActionMediator _actionMediator;
        private readonly IProHelper _proHelper;

        public PassStrategy(IActionMediator actionMediator, IProHelper proHelper)
        {
            _actionMediator = actionMediator;
            _proHelper = proHelper;
        }

        public void Execute(decimal p, int r, PlayerAction playerAction, Skills usedSkills, bool nonCriticalFailure = false)
        {
            var player = playerAction.Player;
            var (lonerSuccess, proSuccess, canUseSkill) = player;
            var pass = (Pass) playerAction.Action;
            var success = pass.Success;
            var failure = pass.Failure;
            var i = playerAction.Index;

            _actionMediator.Resolve(p * success, r, i, usedSkills);

            var inaccuratePass = pass.InaccuratePass;
            var rerollInaccuratePass = pass.RerollInaccuratePass;
            var accuratePassAfterFailure = (failure + (rerollInaccuratePass ? inaccuratePass : 0)) * success;
            var inaccuratePassAfterFailure = (failure + (rerollInaccuratePass ? inaccuratePass : 0)) * inaccuratePass;
            var inaccuratePassWithoutReroll = rerollInaccuratePass ? 0m : inaccuratePass;

            if (canUseSkill(Skills.Pass, usedSkills))
            {
                ExecuteReroll(p, r, i, usedSkills, accuratePassAfterFailure, inaccuratePassWithoutReroll + inaccuratePassAfterFailure);
                return;
            }

            if (_proHelper.UsePro(player, pass, r, usedSkills, success, success))
            {
                _actionMediator.Resolve(inaccuratePassWithoutReroll, r, i, usedSkills, true);

                usedSkills |= Skills.Pro;
                ExecuteReroll(p * proSuccess, r, i, usedSkills | Skills.Pro, accuratePassAfterFailure, inaccuratePassAfterFailure);
                return;
            }

            if (r > 0 && rerollInaccuratePass)
            {
                ExecuteReroll(p * lonerSuccess, r - 1, i, usedSkills | Skills.Pro, accuratePassAfterFailure, inaccuratePassAfterFailure);
                _actionMediator.Resolve(p * inaccuratePass * (1 - lonerSuccess), r - 1, i, usedSkills, true);
                return;
            }

            _actionMediator.Resolve(p * inaccuratePass, r, i, usedSkills, true);
        }

        private void ExecuteReroll(decimal p, int r, int i, Skills usedSkills, decimal accuratePass, decimal inaccuratePass)
        {
            _actionMediator.Resolve(p * accuratePass, r, i, usedSkills);
            _actionMediator.Resolve(p * inaccuratePass, r, i, usedSkills, true);
        }
    }
}