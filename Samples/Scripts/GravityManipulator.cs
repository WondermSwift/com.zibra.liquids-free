using UnityEngine;
using com.zibra.liquid.Solver;

namespace com.zibra.liquid.Samples
{
    public class GravityManipulator : MonoBehaviour
    {
        ZibraLiquid liquid;

        // Start is called before the first frame update
        void Start()
        {
            liquid = GetComponent<ZibraLiquid>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                liquid.solverParameters.Gravity.y = 9.81f;
                liquid.solverParameters.Gravity.x = 0.0f;
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                liquid.solverParameters.Gravity.y = -9.81f;
                liquid.solverParameters.Gravity.x = 0.0f;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                liquid.solverParameters.Gravity.y = 0.0f;
                liquid.solverParameters.Gravity.x = 9.81f;
            }

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                liquid.solverParameters.Gravity.x = -9.81f;
                liquid.solverParameters.Gravity.y = 0.0f;
            }

            if (Input.GetKey(KeyCode.O))
            {
                liquid.solverParameters.Gravity.x = 0.0f;
                liquid.solverParameters.Gravity.y = 0.0f;
            }
        }
    }
}
