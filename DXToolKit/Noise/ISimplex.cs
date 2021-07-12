namespace DXToolKit {
	public interface ISimplex {
		double Evaluate(double x, double y);
		double Evaluate(double x, double y, double z);
		double Evaluate(double x, double y, double z, double w);
	}
}