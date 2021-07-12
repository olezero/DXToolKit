using System;
using SharpDX;

namespace DXToolKit {
	public enum SimplexGenerator {
		OpenSimplex,
		Smooth,
		Fast,
	}

	/// <summary>
	/// Noise class used to generate perlin, billow and Ridged Multi fractal noise 
	/// </summary>
	public class Noise {
		#region Private Fields

		private ISimplex m_simplex;
		private OpenSimplexNoise m_noiseGenerator;
		private OpenSimplex2F m_noiseGen2F;
		private OpenSimplex2S m_noiseGen2S;

		private int m_octaves = 12;
		private float m_frequency = 1.0f;
		private float m_lacunarity = 2.0f;
		private float m_persistence = 0.5f;
		private float[] m_weights;
		private int m_seed;

		#endregion Private Fields

		#region Public Properties

		/// <summary>
		/// Gets or sets the octave count for all the noise generators.
		/// Default 12
		/// </summary>
		public int Octaves {
			get { return m_octaves; }
			set {
				m_octaves = value;
				UpdateWeights();
			}
		}

		/// <summary>
		/// Gets or sets the frequency of the noise.
		/// Default 1.0
		/// </summary>
		public float Frequency {
			get { return m_frequency; }
			set { m_frequency = value; }
		}

		/// <summary>
		/// Gets or sets the lacunarity of the noise.
		/// Default 2.0
		/// </summary>
		public float Lacunarity {
			get { return m_lacunarity; }
			set {
				m_lacunarity = value;
				UpdateWeights();
			}
		}

		/// <summary>
		/// Gets or sets the persistence of the noise.
		/// Default 0.5
		/// </summary>
		public float Persistence {
			get { return m_persistence; }
			set { m_persistence = value; }
		}

		/// <summary>
		/// Gets or sets the seed of the noise.
		/// </summary>
		public int Seed {
			get { return m_seed; }
			set {
				m_seed = value;
				m_noiseGenerator = new OpenSimplexNoise(value);
				m_noiseGen2F = new OpenSimplex2F(value);
				m_noiseGen2S = new OpenSimplex2S(value);
			}
		}

		#endregion Public Properties

		#region Public Constructors

		/// <summary>
		/// Creates a new instance of the noise generator
		/// </summary>
		public Noise() {
			m_noiseGenerator = new OpenSimplexNoise(DateTime.Now.Ticks);
			m_noiseGen2F = new OpenSimplex2F(DateTime.Now.Ticks);
			m_noiseGen2S = new OpenSimplex2S(DateTime.Now.Ticks);
			m_simplex = m_noiseGenerator;
			UpdateWeights();
		}

		/// <summary>
		/// Creates a new instance of the noise generator with a seed
		/// </summary>
		/// <param name="seed">The seed to use for the noise generator</param>
		public Noise(int seed) {
			m_noiseGenerator = new OpenSimplexNoise(seed);
			m_noiseGen2F = new OpenSimplex2F(seed);
			m_noiseGen2S = new OpenSimplex2S(seed);
			m_simplex = m_noiseGenerator;
			UpdateWeights();
		}

		#endregion Public Constructors

		#region Public Methods

		/// <summary>
		/// Generates 2D perlin noise at the given coordinates
		/// </summary>
		/// <param name="x">X position</param>
		/// <param name="y">Y position</param>
		/// <returns>Noise at input coordinate</returns>
		public float Perlin(float x, float y) {
			float value = 0.0f;
			float cp = 1.0f;
			x *= m_frequency;
			y *= m_frequency;
			for (var i = 0; i < m_octaves; i++) {
				float signal = (float) m_simplex.Evaluate(x, y);
				value += signal * cp;
				x *= m_lacunarity;
				y *= m_lacunarity;
				cp *= m_persistence;
			}

			return value;
		}

		/// <summary>
		/// Generates 3D perlin noise at the given coordinates
		/// </summary>
		/// <param name="x">X position</param>
		/// <param name="y">Y position</param>
		/// <param name="z">Z position</param>
		/// <returns>Noise at input coordinate</returns>
		public float Perlin(float x, float y, float z) {
			float value = 0.0f;
			float cp = 1.0f;
			x *= m_frequency;
			y *= m_frequency;
			z *= m_frequency;
			for (var i = 0; i < m_octaves; i++) {
				float signal = (float) m_simplex.Evaluate(x, y, z);
				value += signal * cp;
				x *= m_lacunarity;
				y *= m_lacunarity;
				z *= m_lacunarity;
				cp *= m_persistence;
			}

			return value;
		}

		/// <summary>
		/// Generates 2D Billow noise at the given coordinates
		/// </summary>
		/// <param name="x">X position</param>
		/// <param name="y">Y position</param>
		/// <returns>Noise at input coordinate</returns>
		public float Billow(float x, float y) {
			var value = 0.0f;
			var cp = 1.0f;
			x *= m_frequency;
			y *= m_frequency;
			for (var i = 0; i < m_octaves; i++) {
				var signal = (float) m_simplex.Evaluate(x, y);
				signal = 2.0f * Math.Abs(signal) - 1.0f;
				value += signal * cp;
				x *= m_lacunarity;
				y *= m_lacunarity;
				cp *= m_persistence;
			}

			return value + 0.5f;
		}

		/// <summary>
		/// Generates 3D Billow noise at the given coordinates
		/// </summary>
		/// <param name="x">X position</param>
		/// <param name="y">Y position</param>
		/// <param name="z">Z position</param>
		/// <returns>Noise at input coordinate</returns>
		public float Billow(float x, float y, float z) {
			var value = 0.0f;
			var cp = 1.0f;
			x *= m_frequency;
			y *= m_frequency;
			z *= m_frequency;
			for (var i = 0; i < m_octaves; i++) {
				var signal = (float) m_simplex.Evaluate(x, y, z);
				signal = 2.0f * Math.Abs(signal) - 1.0f;
				value += signal * cp;
				x *= m_lacunarity;
				y *= m_lacunarity;
				z *= m_lacunarity;
				cp *= m_persistence;
			}

			return value + 0.5f;
		}

		/// <summary>
		/// Generates 2D RidgedMultifractal noise at the given coordinates
		/// </summary>
		/// <param name="x">X position</param>
		/// <param name="y">Y position</param>
		/// <returns>Noise at input coordinate</returns>
		public float RidgedMultifractal(float x, float y) {
			x *= m_frequency;
			y *= m_frequency;
			float value = 0.0f;
			float weight = 1.0f;
			float offset = 1.0f; // TODO: Review why Offset is never assigned
			float gain = 2.0f; // TODO: Review why gain is never assigned
			for (var i = 0; i < m_octaves; i++) {
				var signal = (float) m_simplex.Evaluate(x, y);
				signal = Math.Abs(signal);
				signal = offset - signal;
				signal *= signal;
				signal *= weight;
				weight = signal * gain;
				weight = MathUtil.Clamp(weight, 0, 1);
				value += (signal * m_weights[i]);
				x *= m_lacunarity;
				y *= m_lacunarity;
			}

			return (value * 1.25f) - 1.0f;
		}


		/// <summary>
		/// Generates 3D RidgedMultifractal noise at the given coordinates
		/// </summary>
		/// <param name="x">X position</param>
		/// <param name="y">Y position</param>
		/// <param name="z">Z position</param>
		/// <returns>Noise at input coordinate</returns>
		public float RidgedMultifractal(float x, float y, float z) {
			x *= m_frequency;
			y *= m_frequency;
			z *= m_frequency;
			float value = 0.0f;
			float weight = 1.0f;
			float offset = 1.0f; // TODO: Review why Offset is never assigned
			float gain = 2.0f; // TODO: Review why gain is never assigned
			for (var i = 0; i < m_octaves; i++) {
				var signal = (float) m_simplex.Evaluate(x, y, z);
				signal = Math.Abs(signal);
				signal = offset - signal;
				signal *= signal;
				signal *= weight;
				weight = signal * gain;
				weight = MathUtil.Clamp(weight, 0, 1);
				value += (signal * m_weights[i]);
				x *= m_lacunarity;
				y *= m_lacunarity;
				z *= m_lacunarity;
			}

			return (value * 1.25f) - 1.0f;
		}


		/// <summary>
		/// Overload of base perlin to take a Vector2 position
		/// </summary>
		/// <param name="position">Position as a vector2</param>
		/// <returns>Noise at input coordinate</returns>
		public float Perlin(Vector2 position) {
			return Perlin(position.X, position.Y);
		}

		/// <summary>
		/// Overload of base perlin to take a Vector3 position
		/// </summary>
		/// <param name="position">Position as a vector3</param>
		/// <returns>Noise at input coordinate</returns>
		public float Perlin(Vector3 position) {
			return Perlin(position.X, position.Y, position.Z);
		}

		/// <summary>
		/// Overload of base Billow to take a Vector2 position
		/// </summary>
		/// <param name="position">Position as a vector2</param>
		/// <returns>Noise at input coordinate</returns>
		public float Billow(Vector2 position) {
			return Billow(position.X, position.Y);
		}

		/// <summary>
		/// Overload of base Billow to take a Vector3 position
		/// </summary>
		/// <param name="position">Position as a vector3</param>
		/// <returns>Noise at input coordinate</returns>
		public float Billow(Vector3 position) {
			return Billow(position.X, position.Y, position.Z);
		}

		/// <summary>
		/// Overload of base RidgedMultifractal to take a Vector2 position
		/// </summary>
		/// <param name="position">Position as a vector2</param>
		/// <returns>Noise at input coordinate</returns>
		public float RidgedMultifractal(Vector2 position) {
			return RidgedMultifractal(position.X, position.Y);
		}

		/// <summary>
		/// Overload of base RidgedMultifractal to take a Vector3 position
		/// </summary>
		/// <param name="position">Position as a vector3</param>
		/// <returns>Noise at input coordinate</returns>
		public float RidgedMultifractal(Vector3 position) {
			return RidgedMultifractal(position.X, position.Y, position.Z);
		}

		/// <summary>
		/// Sets the simplex generator to use for the base noise algorithm
		/// </summary>
		/// <param name="generator"></param>
		public void SetSimplexGenerator(SimplexGenerator generator) {
			switch (generator) {
				case SimplexGenerator.OpenSimplex:
					m_simplex = m_noiseGenerator;
					break;
				case SimplexGenerator.Smooth:
					m_simplex = m_noiseGen2S;
					break;
				case SimplexGenerator.Fast:
					m_simplex = m_noiseGen2F;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(generator), generator, null);
			}
		}

		#endregion Public Methods

		#region Private Methods

		private void UpdateWeights() {
			m_weights = new float[m_octaves];

			var f = 1.0;
			for (var i = 0; i < m_weights.Length; i++) {
				m_weights[i] = (float) Math.Pow(f, -1.0);
				f *= m_lacunarity;
			}
		}

		#endregion Private Methods
	}
}