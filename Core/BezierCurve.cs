namespace CalamityFables.Core
{
    public class BezierCurve
    {
        public Vector2[] ControlPoints;
        public float[] arcLenghts;

        public BezierCurve(params Vector2[] controls)
        {
            ControlPoints = controls;
        }


        public Vector2 Evaluate(float interpolant) => PrivateEvaluate(ControlPoints, MathHelper.Clamp(interpolant, 0f, 1f));

        public List<Vector2> GetPoints(int totalPoints)
        {
            float perStep = 1f / totalPoints;

            List<Vector2> points = new List<Vector2>();

            for (float step = 0f; step <= 1f; step += perStep)
                points.Add(Evaluate(step));

            return points;
        }

        public float ArcLentghParametrize(float step, float totalCurveLentgh)
        {
            float pointAtLentgh = step * totalCurveLentgh;

            float longestLenghtFound = 0;
            float longerLenghtFound = 0;


            int index = 0;

            for (int i = 0; i < arcLenghts.Length; i++)
            {
                if (arcLenghts[i] == pointAtLentgh)
                    return i / (float)(arcLenghts.Length - 1);

                if (arcLenghts[i] > pointAtLentgh)
                {
                    longerLenghtFound = arcLenghts[i];
                    break;
                }

                index = i;
                longestLenghtFound = arcLenghts[i];
            }

            if (longerLenghtFound != 0)
            {
                return (index + (pointAtLentgh - longestLenghtFound) / (longerLenghtFound - longestLenghtFound)) / (float)(arcLenghts.Length - 1);
            }

            return 1;
        }

        public List<Vector2> GetEvenlySpacedPoints(int totalPoints, int computationPrecision = 30, bool forceRecalculate = false)
        {
            if (arcLenghts == null || arcLenghts.Length == 0 || forceRecalculate)
            {
                arcLenghts = new float[computationPrecision + 1];
                arcLenghts[0] = 0;

                //Calculate the arc lentgh at a bunch of points
                Vector2 oldPosition = ControlPoints[0];
                for (int i = 1; i <= computationPrecision; i += 1)
                {
                    Vector2 position = Evaluate(i / (float)computationPrecision);
                    float curveLength = (position - oldPosition).Length();
                    arcLenghts[i] = arcLenghts[i - 1] + curveLength;

                    oldPosition = position;
                }
            }


            float totalCurveLentgh = arcLenghts[arcLenghts.Length - 1];


            List<Vector2> points = new List<Vector2>();

            for (int step = 0; step < totalPoints; step++)
                points.Add(Evaluate(ArcLentghParametrize((step / (float)(totalPoints - 1)), totalCurveLentgh)));

            return points;
        }

        private Vector2 PrivateEvaluate(Vector2[] points, float T)
        {
            while (points.Length > 2)
            {
                Vector2[] nextPoints = new Vector2[points.Length - 1];
                for (int k = 0; k < points.Length - 1; k++)
                    nextPoints[k] = Vector2.Lerp(points[k], points[k + 1], T);

                points = nextPoints;
            }

            if (points.Length <= 1)
                return Vector2.Zero;

            return Vector2.Lerp(points[0], points[1], T);
        }
    }
}
