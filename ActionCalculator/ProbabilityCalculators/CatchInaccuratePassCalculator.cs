﻿using ActionCalculator.Abstractions;
using ActionCalculator.Abstractions.ProbabilityCalculators;

namespace ActionCalculator.ProbabilityCalculators
{
	public class CatchInaccuratePassCalculator : IProbabilityCalculator
	{
		private readonly IProbabilityCalculator _probabilityCalculator;
		private readonly IProCalculator _proCalculator;

		private const decimal ScatterToTarget = 24m / 512;
		private const decimal ScatterToTargetOrAdjacent = 240m / 512;
		private const decimal ScatterThenBounceToTarget = (ScatterToTargetOrAdjacent - ScatterToTarget) / 8;

		public CatchInaccuratePassCalculator(IProbabilityCalculator probabilityCalculator, IProCalculator proCalculator)
		{
			_probabilityCalculator = probabilityCalculator;
			_proCalculator = proCalculator;
		}

		public void Calculate(decimal p, int r, PlayerAction playerAction, Skills usedSkills, bool inaccuratePass = false)
		{
			var player = playerAction.Player;
			var action = playerAction.Action;

			var roll = action.OriginalRoll + 1;
			roll += player.HasSkill(Skills.DivingCatch) ? 1 : 0;

			var catchSuccess = (7m - roll.ThisOrMinimum(2).ThisOrMaximum(6)) / 6;
			var catchFailure = 1 - catchSuccess;

			CatchScatteredPass(p, r, playerAction, usedSkills, catchSuccess, catchFailure);
			CatchBouncingBall(p, r, playerAction, usedSkills, catchSuccess, catchFailure);
		}

		private void CatchScatteredPass(decimal p, int r, PlayerAction playerAction, Skills usedSkills,
			decimal catchSuccess, decimal catchFailure)
		{
			var player = playerAction.Player;
			var successfulScatter = player.HasSkill(Skills.DivingCatch)
				? ScatterToTargetOrAdjacent
				: ScatterToTarget;

			CalculateCatch(p, r, playerAction, usedSkills, successfulScatter * catchSuccess,
				successfulScatter * catchFailure * catchSuccess);
		}

		private void CatchBouncingBall(decimal p, int r, PlayerAction playerAction, Skills usedSkills,
			decimal catchSuccess, decimal catchFailure)
		{
			var player = playerAction.Player;

			if (player.HasSkill(Skills.DivingCatch))
			{
				CalculateDivingCatch(p, r, playerAction, usedSkills, catchSuccess, catchFailure);
				return;
			}

			CalculateCatch(p, r, playerAction, usedSkills, ScatterThenBounceToTarget * catchSuccess,
				ScatterThenBounceToTarget * catchFailure * catchSuccess);
		}

		private void CalculateDivingCatch(decimal p, int r, PlayerAction playerAction, Skills usedSkills, decimal catchSuccess, decimal catchFailure)
		{
			var failDivingCatch = catchFailure * catchFailure;

			var player = playerAction.Player;

			if (player.HasSkill(Skills.Catch))
			{
				_probabilityCalculator.Calculate(p * failDivingCatch * ScatterThenBounceToTarget * (catchFailure * catchSuccess + catchSuccess),
					r, playerAction, usedSkills);

				return;
			}

			if (_proCalculator.UsePro(playerAction, r, usedSkills))
			{
				_probabilityCalculator.Calculate(
					p * failDivingCatch * player.ProSuccess * ScatterThenBounceToTarget * catchSuccess, r, playerAction,
					usedSkills | Skills.Pro);

				if (r > 0)
				{
					_probabilityCalculator.Calculate(p * failDivingCatch * player.ProSuccess * ScatterThenBounceToTarget * catchFailure * player.LonerSuccess * catchSuccess, 
						r - 1, playerAction, usedSkills | Skills.Pro);
				}

				return;
			}

			if (r > 0)
			{
				_probabilityCalculator.Calculate(p * failDivingCatch * player.LonerSuccess * ScatterThenBounceToTarget * catchSuccess, 
					r - 1, playerAction, usedSkills);

				if (r > 1)
				{
					_probabilityCalculator.Calculate(p * failDivingCatch * player.LonerSuccess * ScatterThenBounceToTarget
					                                 * catchFailure * player.LonerSuccess * catchSuccess, r - 2, playerAction,
						usedSkills);
				}

				return;
			}

			_probabilityCalculator.Calculate(p * catchFailure * ScatterThenBounceToTarget * catchSuccess, r, playerAction,
				usedSkills);
		}

		private void CalculateCatch(decimal p, int r, PlayerAction playerAction, Skills usedSkills,
			decimal successNoReroll, decimal successWithReroll)
		{
			_probabilityCalculator.Calculate(p * successNoReroll, r, playerAction, usedSkills);

			p *= successWithReroll;

			var player = playerAction.Player;
			if (player.HasSkill(Skills.Catch))
			{
				_probabilityCalculator.Calculate(p, r, playerAction, usedSkills);
				return;
			}
			
			if (_proCalculator.UsePro(playerAction, r, usedSkills))
			{
				_probabilityCalculator.Calculate(p * player.ProSuccess, r, playerAction, usedSkills | Skills.Pro);
				return;
			}
			
			if (r > 0)
			{
				_probabilityCalculator.Calculate(p * player.LonerSuccess, r - 1, playerAction, usedSkills);
			}
		}
	}
}