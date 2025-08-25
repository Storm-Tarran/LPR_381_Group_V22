using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR_381_Group_V22.SensitivityAnalysis
{
    internal class SensitivityAnalyzer
    {
        public void DisplayRangeNonBasic() { 
            
           
        
        }
        public void ChangeNonBasic() {

            Console.Write("Enter index of Non-Basic Variable to change: ");
            int index = int.Parse(Console.ReadLine());
            Console.Write("Enter new value: ");
            double newVal = double.Parse(Console.ReadLine());
        }
        public void DisplayRangeBasic() { 
             
        }
        public void ChangeBasic() {

            Console.Write("Enter Basic Variable index to change: ");
            int index = int.Parse(Console.ReadLine());
            Console.Write("Enter new RHS value: ");
            double newVal = double.Parse(Console.ReadLine());

        }
        public void DisplayRangeRHS() { 
            
        }
        public void ChangeRHS() {

            Console.Write("Enter constraint index to change RHS: ");
            int index = int.Parse(Console.ReadLine());
            Console.Write("Enter new RHS value: ");
            double newVal = double.Parse(Console.ReadLine());

        }
        public void DisplayRangeNonBasicColumn() { 
        
        }
        public void ChangeNonBasicColumn() {

            Console.Write("Enter Non-Basic Variable column index: ");
            int col = int.Parse(Console.ReadLine());
            Console.Write("Enter row to change: ");
            int row = int.Parse(Console.ReadLine());
            Console.Write("Enter new value: ");
            double val = double.Parse(Console.ReadLine());

        }
        public void AddNewActivity() { 
        
        }
        public void AddNewConstraint() { 
        
        }
        public void DisplayShadowPrices() { 
        
        }
        public void PerformDuality() { 
        
        }
    }
}
