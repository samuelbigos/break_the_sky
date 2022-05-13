#ifndef GDANL_H
#define GDANL_H

#include <Godot.hpp>
#include <Node.hpp>

namespace godot {

class ANL : public Node 
{
    GODOT_CLASS(ANL, Node)

private:

    void* m_img;

public:
    static void _register_methods();

    ANL();
    ~ANL();

    void _init();
    
    void Generate3DGradientNoiseImage(int, float, int);
    double SampleGradientImage(int, int, int);
};

}

#endif