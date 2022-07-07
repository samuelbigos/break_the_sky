#ifndef GDANL_H
#define GDANL_H

#include <Godot.hpp>
#include <Node.hpp>
#include <vector>

namespace godot {

class ANL : public Node 
{
    GODOT_CLASS(ANL, Node)

private:

    void* m_img;

    std::vector<void*> m_img_handles_2d;
    std::vector<void*> m_img_handles_3d;

public:
    static void _register_methods();

    ANL();
    ~ANL();

    void _init();
    
    int Generate2DGradientNoiseImage(int, float, float, int, int);
    void ReleaseNoiseImage2D(int);
    double SampleGradientImage2D(int, int, int);

    int Generate3DGradientNoiseImage(int, float, float, float, int, int);
    void ReleaseNoiseImage3D(int);
    double SampleGradientImage3D(int, int, int, int);
};

}

#endif