using System;
using System.Collections.Generic;
using System.Data;
using SymServices.GF;
using SymViewModel.Common;
using SymViewModel.PF;

namespace SymRepository.GF
{
    public class GFProfitDistributionNewRepo
    {
       
        public List<ProfitDistributionNewVM> SelectAll(int Id = 0, string[] conditionFields = null, string[] conditionValues = null)
        {
            try
            {
                return new GFProfitDistributionNewDAL().SelectAll(Id, conditionFields, conditionValues);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public List<ProfitDistributionNewVM> SelectAllGF(int Id = 0, string[] conditionFields = null, string[] conditionValues = null)
        {
            try
            {
                return new GFProfitDistributionNewDAL().SelectAll(Id, conditionFields, conditionValues, null, null, true);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public List<ProfitDistributionNewVM> SelectForEdit(int Id = 0, string[] conditionFields = null, string[] conditionValues = null)
        {
            try
            {
                return new GFProfitDistributionNewDAL().SelectForEdit(Id, conditionFields, conditionValues);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public List<ProfitDistributionNewVM> SelectForEditGF(int Id = 0, string[] conditionFields = null, string[] conditionValues = null)
        {
            try
            {
                return new GFProfitDistributionNewDAL().SelectForEdit(Id, conditionFields, conditionValues, null, null, true);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public ResultVM Process(ProfitDistributionNewVM vm)
        {
            try
            {
                GFProfitDistributionNewDAL profitDistributionNewDal = new GFProfitDistributionNewDAL();

                return profitDistributionNewDal.Process(vm);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }      


        public string[] ImportExcelFile(ProfitDistributionNewVM vm)
        {
            try
            {
                GFProfitDistributionNewDAL dal = new GFProfitDistributionNewDAL();
                return dal.InsertExportData(vm, null, null);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public string[] Update(ProfitDistributionNewVM vm)
        {
            try
            {
                return new GFProfitDistributionNewDAL().Update(vm);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public string[] UpdateGF(ProfitDistributionNewVM vm)
        {
            try
            {
                return new GFProfitDistributionNewDAL().Update(vm, null, null, true);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string[] Delete(ProfitDistributionNewVM vm)
        {
             try
            {
                return new GFProfitDistributionNewDAL().Delete(vm);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public string[] DeleteGF(ProfitDistributionNewVM vm)
        {
             try
            {
                return new GFProfitDistributionNewDAL().Delete(vm, null, null, true);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
