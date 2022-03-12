using ActionCalculator.Abstractions;
using ActionCalculator.Calculators;
using Xunit;

namespace ActionCalculator.Tests
{
    public class ActionCalculatorTests
    {
        private readonly IActionCalculator _actionCalculator;

        public ActionCalculatorTests()
        {
            _actionCalculator = new ActionCalculator(
                new CalculationBuilder(
                    new ActionBuilder(new TwoD6()), 
                    new PlayerParser()), 
                new ProbabilityComparer(),
                new MasterCalculator(
                    new CalculatorFactory()));
        }

        [Theory]
        [InlineData("2", 1, 0.83333, 0.97222)]
        [InlineData("2,2", 2, 0.69444, 0.92593, 0.94522)]
        [InlineData("D2,D2:D", 1, 0.92593, 0.94522)]
        [InlineData("(D2:D)(D2:D)", 0, 0.94522)]
        [InlineData("5", 1, 0.33333, 0.55556)]
        [InlineData("2,3,4,5,6", 5, 0.01543, 0.05401, 0.09045, 0.10652, 0.10979, 0.11003)]
        [InlineData("1D5", 1, 0.83333, 0.97222)]
        [InlineData("2D5", 1, 0.97222, 0.99923)]
        [InlineData("-3D1", 1, 0.00463, 0.00924)]
        [InlineData("-2D2,2,2D3", 3, 0.06944, 0.16011, 0.18872, 0.19129)]
        [InlineData("(2,3,4,5,6)", 5, 0.01543, 0.05401, 0.09045, 0.10652, 0.10979, 0.11003)]
        [InlineData("(2,3)(4,5,6)", 5, 0.01543, 0.05401, 0.09045, 0.10652, 0.10979, 0.11003)]
        [InlineData("(U2,R2,R2,R2:SH,SF)", 2, 0.84394, 0.89083, 0.89343)]
        [InlineData("C3:C", 0, 0.88889)]
        [InlineData("2,2:L4", 2, 0.69444, 0.81019, 0.81501)]
		[InlineData("2,2,2,2:L3", 4, 0.48225, 0.69659, 0.73231, 0.73496, 0.73503)]
		[InlineData("(P2:PA)(C2)", 1, 0.81019, 0.94522)]
        [InlineData("(P2,I6:PA)(C2)", 1, 0.67515, 0.78768)]
        [InlineData("(P2,I6:PA,CB)(C2)", 1, 0.78768, 0.91896)]
        [InlineData("(P4-2)(C2)", 2, 0.13889, 0.29622, 0.32166)]
        [InlineData("(P4-2)(C2)", 0, 0.16102)]
        [InlineData("(P4-2,I6)(C2)", 0, 0.13050)]
        [InlineData("(P4-2,I7)(C2)", 0, 0.13419)]
        [InlineData("(P4-2)(C1)", 0, 0.16656)]
        [InlineData("(P4-2)(C2:DC)", 0, 0.22141)]
        [InlineData("(P4-2)(C2:DC,C)", 0, 0.28252)]
        [InlineData("(2,3:P4)", 1, 0.55556, 0.84877)]
        [InlineData("(2,3,4,5,6:P4)", 0, 0.03472)]
        [InlineData("(2,3,4,5,6:P4)", 1, 0.01543, 0.07223)]
        [InlineData("(2,3,4,5,6:P4)", 5, 0.01543, 0.05401, 0.09045, 0.10652, 0.10979, 0.11003)]
        [InlineData("1D1:B", 0, 0.19444)]
        [InlineData("1D1:B", 1, 0.19444, 0.30556)]
        [InlineData("-2D2:B", 1, 0.14815, 0.23457)]
        [InlineData("-2D2:B,P3", 0, 0.23663)]
        [InlineData("-2D2:B,P3", 1, 0.22222, 0.27160)]
        [InlineData("-2D2^*:B,P3", 1, 0.23663, 0.26440)]
        [InlineData("L2,2D2|1D2", 0, 0.51852)]
        [InlineData("L2',2D2|1D2", 1, 0.51852, 0.76132)]
        [InlineData("L2,2D2|1D2", 1, 0.46296, 0.75514)]
        [InlineData("L2,2D2|1D2", 2, 0.46296, 0.75514, 0.79561)]
        [InlineData("L3'*,2D2|-2D2:P", 1, 0.47325, 0.69273)]
        [InlineData("F8", 0, 0.41667)]
        [InlineData("F8:S", 0, 0.41667)]
        [InlineData("F8:DP", 0, 0.58333)]
        [InlineData("F8~:DP", 0, 0.31250)]
        [InlineData("F8~:DP,S", 0, 0.39815)]
        [InlineData("F8,2", 0, 0.26524)]
        [InlineData("F8,2:SG", 0, 0.28935)]
        [InlineData("F8,A6,2", 0, 0.27890)]
        [InlineData("F8,E,2", 0, 0.33356)]
        [InlineData("F8,E,A6,2", 0, 0.33584)]
        [InlineData("F8,A6,E,2", 0, 0.32445)]
        [InlineData("2D3,B8", 0, 0.31250)]
        public void ActionCalculatorReturnsExpectedResult(string calculation, int rerolls, params double[] expected)
        {
            var result = _actionCalculator.Calculate(calculation);

            Assert.Equal(expected.Length, result.ProbabilityResults[rerolls].Probabilities.Length);

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal((decimal) expected[i], result.ProbabilityResults[rerolls].Probabilities[i], 5);
            }
        }
    }
}
