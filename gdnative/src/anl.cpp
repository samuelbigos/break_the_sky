#include "anl.h"

#define ANL_IMPLEMENTATION
#define IMPLEMENT_STB
#include "accidental-noise-library-master/anl.h"

using namespace godot;

void ANL::_register_methods() {
    register_method("Generate2DGradientNoiseImage", &ANL::Generate2DGradientNoiseImage);
    register_method("ReleaseNoiseImage2D", &ANL::ReleaseNoiseImage2D);
    register_method("SampleGradientImage2D", &ANL::SampleGradientImage2D);
    register_method("Generate3DGradientNoiseImage", &ANL::Generate3DGradientNoiseImage);
    register_method("ReleaseNoiseImage3D", &ANL::ReleaseNoiseImage3D);
    register_method("SampleGradientImage3D", &ANL::SampleGradientImage3D); 
}

ANL::ANL() 
{
    m_img_handles_2d = {};
    m_img_handles_3d = {};
}

ANL::~ANL() 
{
    for (int i = 0; i < m_img_handles_2d.size(); i++)
    {
        ReleaseNoiseImage2D(i);
    }
    for (int i = 0; i < m_img_handles_3d.size(); i++)
    {
        ReleaseNoiseImage3D(i);
    }
}

void ANL::_init() {
}

int ANL::Generate2DGradientNoiseImage(int dims, float freqX, float freqY, int octaves, int seed)
{
    anl::CKernel kernel;
    anl::CArray2Dd* image = new anl::CArray2Dd(dims, dims);
    //anl::CInstructionIndex b = kernel.gradientBasis(kernel.constant(7), kernel.seed(seed));

    anl::CInstructionIndex b = kernel.simplefBm(anl::BASIS_GRADIENT, anl::INTERP_QUINTIC, octaves, 1.0, seed);
    anl::map2DNoZ(anl::SEAMLESS_XY, *((anl::CArray2Dd*)image), kernel, anl::SMappingRanges(0.0f, freqX, 0.0f, freqY), b);

    m_img_handles_2d.push_back(image);
    return m_img_handles_2d.size() - 1;
}

int ANL::Generate3DGradientNoiseImage(int dims, float freqX, float freqY, float freqZ, int octaves, int seed)
{
    anl::CKernel kernel;
    anl::CArray3Dd* image = new anl::CArray3Dd(dims, dims, dims);
    //anl::CInstructionIndex b = kernel.gradientBasis(kernel.constant(7), kernel.seed(seed));

    anl::CInstructionIndex b = kernel.simplefBm(anl::BASIS_GRADIENT, anl::INTERP_QUINTIC, octaves, 1.0, seed);
    anl::map3D(anl::SEAMLESS_XYZ, *((anl::CArray3Dd*)image), kernel, anl::SMappingRanges(0.0f, freqX, 0.0f, freqY, 0.0f, freqZ), b);

    m_img_handles_3d.push_back(image);
    return m_img_handles_3d.size() - 1;
}

double ANL::SampleGradientImage2D(int handle, int x, int y)
{
    if (handle >= m_img_handles_2d.size())
        return 0.0f;

    anl::CArray2Dd* image = (anl::CArray2Dd*)m_img_handles_2d[handle];

    if (image == 0)
        return 0.0f;

    return image->get(x, y);
}

double ANL::SampleGradientImage3D(int handle, int x, int y, int z)
{
    if (handle >= m_img_handles_3d.size())
        return 0.0f;

    anl::CArray3Dd* image = (anl::CArray3Dd*)m_img_handles_3d[handle];

    if (image == 0)
        return 0.0f;

    return image->get(x, y, z);
}

void ANL::ReleaseNoiseImage2D(int handle)
{
    if (handle >= m_img_handles_2d.size())
        return;

    anl::CArray2Dd* image = (anl::CArray2Dd*)m_img_handles_2d[handle];
    if (image != 0)
        delete image;
}

void ANL::ReleaseNoiseImage3D(int handle)
{
    if (handle >= m_img_handles_3d.size())
        return;

    anl::CArray3Dd* image = (anl::CArray3Dd*)m_img_handles_3d[handle];
    if (image != 0)
        delete image;
}