#include "anl.h"

#define ANL_IMPLEMENTATION
#define IMPLEMENT_STB
#include "accidental-noise-library-master/anl.h"

using namespace godot;

void ANL::_register_methods() {
    register_method("Generate2DGradient", &ANL::Generate2DGradient);
    register_method("Generate2DCellular", &ANL::Generate2DCellular);
    register_method("Generate2DCellularFBM", &ANL::Generate2DCellularFBM);

    register_method("Generate3DGradient", &ANL::Generate3DGradient);
    register_method("Generate3DCellular", &ANL::Generate3DCellular);
    register_method("Generate3DCellularFBM", &ANL::Generate3DCellularFBM);

    register_method("Sample2D", &ANL::Sample2D);
    register_method("Sample3D", &ANL::Sample3D);

    register_method("Release2D", &ANL::Release2D);
    register_method("Release3D", &ANL::Release3D);
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
        Release2D(i);
    }
    for (int i = 0; i < m_img_handles_3d.size(); i++)
    {
        Release3D(i);
    }
}

void ANL::_init() {
}

int ANL::Generate2DGradient(int size, float freq, int octaves, int seed)
{
    anl::CKernel k;
    anl::CArray2Dd* image = new anl::CArray2Dd(size, size);

    _gradient(&k, octaves, seed);
    anl::map2DNoZ(anl::SEAMLESS_XY, *((anl::CArray2Dd*)image), k, anl::SMappingRanges(0.0f, freq, 0.0f, freq), k.lastIndex());

    image->scaleToRange(0.0, 1.0);
    m_img_handles_2d.push_back(image);
    return (int)m_img_handles_2d.size() - 1;
}

int ANL::Generate2DCellular(int size, float freq, int seed)
{
    anl::CKernel k;
    anl::CArray2Dd* image = new anl::CArray2Dd(size, size);

    _cellular(&k, seed);
    anl::map2DNoZ(anl::SEAMLESS_XY, *((anl::CArray2Dd*)image), k, anl::SMappingRanges(0.0f, freq, 0.0f, freq), k.lastIndex());

    image->scaleToRange(0.0, 1.0);
    m_img_handles_2d.push_back(image);
    return (int)m_img_handles_2d.size() - 1;
}

int ANL::Generate2DCellularFBM(int size, float freq, int octaves, int seed)
{
    anl::CKernel k;
    anl::CArray2Dd* image = new anl::CArray2Dd(size, size);

    _cellularFbm(&k, seed);
    anl::map2DNoZ(anl::SEAMLESS_XY, *((anl::CArray2Dd*)image), k, anl::SMappingRanges(0.0f, freq, 0.0f, freq), k.lastIndex());

    image->scaleToRange(0.0, 1.0);
    m_img_handles_2d.push_back(image);
    return (int)m_img_handles_2d.size() - 1;
}

int ANL::Generate3DGradient(int size, float freq, int octaves, int seed)
{
    anl::CKernel k;
    anl::CArray3Dd* image = new anl::CArray3Dd(size, size, size);

    _gradient(&k, octaves, seed);
    anl::map3D(anl::SEAMLESS_XYZ, *((anl::CArray3Dd*)image), k, anl::SMappingRanges(0.0f, freq, 0.0f, freq, 0.0f, freq), k.lastIndex());

    image->scaleToRange(0.0, 1.0);
    m_img_handles_3d.push_back(image);
    return (int)m_img_handles_3d.size() - 1;
}

int ANL::Generate3DCellular(int size, float freq, int seed)
{
    anl::CKernel k;
    anl::CArray3Dd* image = new anl::CArray3Dd(size, size, size);

    _cellular(&k, seed);
    anl::map3D(anl::SEAMLESS_XYZ, *((anl::CArray3Dd*)image), k, anl::SMappingRanges(0.0f, freq, 0.0f, freq, 0.0f, freq), k.lastIndex());

    image->scaleToRange(0.0, 1.0);
    m_img_handles_3d.push_back(image);
    return (int)m_img_handles_3d.size() - 1;
}

int ANL::Generate3DCellularFBM(int size, float freq, int octaves, int seed)
{
    anl::CKernel k;
    anl::CArray3Dd* image = new anl::CArray3Dd(size, size, size);

    _cellularFbm(&k, seed);
    anl::map3D(anl::SEAMLESS_XYZ, *((anl::CArray3Dd*)image), k, anl::SMappingRanges(0.0f, freq, 0.0f, freq, 0.0f, freq), k.lastIndex());

    image->scaleToRange(0.0, 1.0);
    m_img_handles_3d.push_back(image);
    return (int)m_img_handles_3d.size() - 1;
}

double ANL::Sample2D(int handle, int x, int y)
{
    if (handle >= m_img_handles_2d.size())
        return 0.0f;

    anl::CArray2Dd* image = (anl::CArray2Dd*)m_img_handles_2d[handle];

    if (image == 0)
        return 0.0f;

    return image->get(x, y);
}

double ANL::Sample3D(int handle, int x, int y, int z)
{
    if (handle >= m_img_handles_3d.size())
        return 0.0f;

    anl::CArray3Dd* image = (anl::CArray3Dd*)m_img_handles_3d[handle];

    if (image == 0)
        return 0.0f;

    return image->get(x, y, z);
}

void ANL::Release2D(int handle)
{
    if (handle >= m_img_handles_2d.size())
        return;

    anl::CArray2Dd* image = (anl::CArray2Dd*)m_img_handles_2d[handle];
    if (image != 0)
        delete image;
}

void ANL::Release3D(int handle)
{
    if (handle >= m_img_handles_3d.size())
        return;

    anl::CArray3Dd* image = (anl::CArray3Dd*)m_img_handles_3d[handle];
    if (image != 0)
        delete image;
}

void ANL::_gradient(anl::CKernel* k, int octaves, int seed)
{
    k->simplefBm(anl::BASIS_GRADIENT, anl::INTERP_QUINTIC, octaves, 1.0, seed); 
}

void ANL::_cellular(anl::CKernel* k, int seed)
{
    k->cellularBasis(k->constant(1), k->zero(), k->zero(), k->zero(), k->zero(), k->zero(), k->zero(), k->zero(), k->constant(0), k->seed(seed));
}

void ANL::_cellularFbm(anl::CKernel* k, int seed)
{
    anl::CInstructionIndex c1 = k->scaleDomain(k->multiply(k->cellularBasis(k->constant(1), k->zero(), k->zero(), k->zero(), k->zero(), k->zero(), k->zero(), k->zero(), k->constant(0), k->seed(seed)), k->constant(.625)), k->constant(1.0));
    anl::CInstructionIndex c2 = k->scaleDomain(k->multiply(k->cellularBasis(k->constant(1), k->zero(), k->zero(), k->zero(), k->zero(), k->zero(), k->zero(), k->zero(), k->constant(0), k->seed(seed+10+1*1000)), k->constant(.25)), k->constant(2.0));
    anl::CInstructionIndex c3 = k->scaleDomain(k->multiply(k->cellularBasis(k->constant(1), k->zero(), k->zero(), k->zero(), k->zero(), k->zero(), k->zero(), k->zero(), k->constant(0), k->seed(seed+10+2*1000)), k->constant(.125)), k->constant(4.0));
    anl::CInstructionIndex a = k->add(c2, c3);
    k->add(c1, a);
}