#include "anl.h"

#define ANL_IMPLEMENTATION
#define IMPLEMENT_STB
#include "accidental-noise-library-master/anl.h"

using namespace godot;

void ANL::_register_methods() {
    register_method("Generate3DGradientNoiseImage", &ANL::Generate3DGradientNoiseImage);
    register_method("SampleGradientImage", &ANL::SampleGradientImage);
}

ANL::ANL() 
{
    m_img = 0;
}

ANL::~ANL() 
{
    if (m_img != 0)
        delete (anl::CArray3Dd*)m_img;
}

void ANL::_init() {
}

void ANL::Generate3DGradientNoiseImage(int dims, float freq, int seed)
{
    anl::CKernel kernel;
    m_img = new anl::CArray3Dd(dims, dims, dims);
    anl::CInstructionIndex b = kernel.gradientBasis(kernel.constant(3), kernel.seed(seed));
    anl::map3D(anl::SEAMLESS_XYZ, *((anl::CArray3Dd*)m_img), kernel, anl::SMappingRanges(0.0f, freq, 0.0f, freq, 0.0f, freq), b);
}

double ANL::SampleGradientImage(int x, int y, int z)
{
    if (m_img == 0)
    {
        return 0.0f;
    }

    return ((anl::CArray3Dd*)m_img)->get(x, y, z);
}