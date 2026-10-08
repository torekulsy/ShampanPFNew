using SymServices.WWF;
using SymServices.Common;

using SymViewModel.Attendance;
using SymViewModel.Common;
using SymViewModel.WWF;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SymRepository.WWF
{
    public class ROIDetailRepo
    {
        public List<ROIDetailVM> SelectById(int Id = 0)
        {
            try
            {
                return new ROIDetailDAL().SelectById(Id);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<ROIDetailVM> SelectByMasterId(int Id = 0)
        {
            try
            {
                return new ROIDetailDAL().SelectByMasterId(Id);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<ROIDetailVM> SelectAll(string[] conditionFields = null, string[] conditionValues = null)
        {
            try
            {
                return new ROIDetailDAL().SelectAll(conditionFields, conditionValues);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


    }
}
