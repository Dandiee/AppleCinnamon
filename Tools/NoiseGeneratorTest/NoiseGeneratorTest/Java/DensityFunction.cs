using NoiseGeneratorTest.Java;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NoiseGeneratorTest.Java
{

    //public interface ContextProvider
    //{
    //    FunctionContext forIndex(int p_208235_);
    //
    //    void fillAllDirectly(double[] p_208236_, DensityFunction p_208237_);
    //}
    //
    //public interface FunctionContext
    //{
    //    int blockX();
    //
    //    int blockY();
    //
    //    int blockZ();
    //
    //    Blender getBlender() => Blender.empty();
    //}
    //
    //public class NoiseHolder
    //{
    //    public Holder<NormalNoise.NoiseParameters> noiseData;
    //    public NormalNoise noise;
    //
    //    public NoiseHolder(Holder<NormalNoise.NoiseParameters> p_224001_, NormalNoise noise)
    //    {
    //        this.noiseData = p_224001_;
    //        this.noise = noise;
    //    }
    //
    //    public NoiseHolder(Holder<NormalNoise.NoiseParameters> p_224001_)
    //     : this(p_224001_, null) { }
    //
    //    public double getValue(double p_224007_, double p_224008_, double p_224009_) =>
    //        this.noise == null ? 0.0D : this.noise.getValue(p_224007_, p_224008_, p_224009_);
    //    public double maxValue() this.noise == null ? 2.0D : this.noise.maxValue();
    //}
    //
    //public interface SimpleFunction : DensityFunction
    //{
    //    void fillArray(double[] p_208241_, ContextProvider p_208242_)
    //    {
    //        p_208242_.fillAllDirectly(p_208241_, this);
    //    }
    //
    //    DensityFunction mapAll(DensityFunction.Visitor p_208239_)
    //    {
    //        return p_208239_.apply(this);
    //    }
    //}
    //
    //public class SinglePointContext : FunctionContext
    //{
    //    public int blockX;
    //    public int blockY;
    //    public int blockZ;
    //
    //    public SinglePointContext(int blockX, int blockY, int blockZ)
    //    {
    //        this.blockX = blockX;
    //        this.blockY = blockY;
    //        this.blockZ = blockZ;
    //    }
    //}

    //public interface Visitor
    //{
    //    DensityFunction apply(DensityFunction p_224019_);
    //    NoiseHolder visitNoise(NoiseHolder p_224018_) => p_224018_;
    //}


    public abstract class DensityFunction
    {
        //double compute(FunctionContext p_208223_);
        //void fillArray(double[] p_208227_, ContextProvider p_208228_);
        //DensityFunction mapAll(DensityFunction.Visitor p_208224_);
        //double minValue();
        //double maxValue();
        //
        //DensityFunction clamp(double p_208221_, double p_208222_) => new DensityFunctions.Clamp(this, p_208221_, p_208222_);
        //DensityFunction abs() => DensityFunctions.map(this, DensityFunctions.Mapped.Type.ABS);
        //DensityFunction square() => DensityFunctions.map(this, DensityFunctions.Mapped.Type.SQUARE);
        //DensityFunction cube() => DensityFunctions.map(this, DensityFunctions.Mapped.Type.CUBE);
        //DensityFunction halfNegative() => DensityFunctions.map(this, DensityFunctions.Mapped.Type.HALF_NEGATIVE);
        //DensityFunction quarterNegative() => DensityFunctions.map(this, DensityFunctions.Mapped.Type.QUARTER_NEGATIVE);
        //DensityFunction squeeze() => DensityFunctions.map(this, DensityFunctions.Mapped.Type.SQUEEZE);
    }
}
