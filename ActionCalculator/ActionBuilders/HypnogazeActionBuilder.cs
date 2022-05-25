﻿using ActionCalculator.Abstractions;
using ActionCalculator.Models.Actions;
using ActionCalculator.Utilities;
using Action = ActionCalculator.Models.Actions.Action;

namespace ActionCalculator.ActionBuilders;

public class HypnogazeActionBuilder : IActionBuilder
{
    public Action Build(string input)
    {
        var usePro = input.Contains("*");
        var rerollFailure = !input.Contains("'");

        input = input.Replace("*", "").Replace("'", "");

        var roll = int.Parse(input.Length == 2 ? input[1..] : input);
        var success = (7m - roll.ThisOrMinimum(2).ThisOrMaximum(6)) / 6;

        return new Hypnogaze(success, 1 - success, roll, usePro, rerollFailure);
    }
}