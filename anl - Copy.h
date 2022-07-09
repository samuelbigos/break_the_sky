#ifndef GDANL_H
#define GDANL_H

#include <Godot.hpp>
#include <Node.hpp>
#include <vector>

namespace anl {
    class CKernel;
    class CInstructionIndex;
}

namespace godot {

class ANL : public Node 
{
    GODOT_CLASS(ANL, Node)

private:

    std::vector<void*> m_img_handles_2d;
    std::vector<void*> m_img_handles_3d;

    void _cellularFractalLayer(void*, double layerscale, double layerfreq, unsigned int s, double angle, double ax, double ay, double az);

    void _gradient(anl::CKernel*, int, int);
    void _cellularFbm(anl::CKernel*, int);

public:
    static void _register_methods();

    ANL();
    ~ANL();

    void _init();
    
    int Generate2DGradient(int, float, int, int);
    int Generate2DCellularFBM(int, float, int, int);
    
    int Generate3DGradient(int, float, int, int);
    int Generate3DCellularFBM(int, float, int, int);
    
    double Sample2D(int, int, int);
    double Sample3D(int, int, int, int);

    void Release2D(int);
    void Release3D(int);
};

}

#endif