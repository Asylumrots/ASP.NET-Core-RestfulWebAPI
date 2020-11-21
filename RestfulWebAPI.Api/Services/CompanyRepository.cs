using Microsoft.EntityFrameworkCore;
using RestfulWebAPI.Api.Data;
using RestfulWebAPI.Api.DtoParameters;
using RestfulWebAPI.Api.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestfulWebAPI.Api.Helpers;
using RestfulWebAPI.Api.Models;

namespace RestfulWebAPI.Api.Services
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly MyDbContext _context;
        private readonly IPropertyMappingService _propertyMappingService;

        public CompanyRepository(MyDbContext context,IPropertyMappingService propertyMappingService)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));//判断是否为空，为空则抛出异常
            _propertyMappingService = propertyMappingService ?? throw new ArgumentNullException(nameof(propertyMappingService));
        }

        public async Task<Company> GetCompanyAsync(Guid companyId)
        {
            if (companyId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(companyId));
            }

            return await _context.Companies
                .FirstOrDefaultAsync(x => x.Id == companyId);
        }

        public async Task<IEnumerable<Company>> GetCompaniesAsync(IEnumerable<Guid> companyIds)
        {
            if (companyIds == null)
            {
                throw new ArgumentNullException(nameof(companyIds));
            }

            return await _context.Companies
                .Where(x => companyIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<PagedList<Company>> GetCompaniesAsync(CompanyDtoParameters parameters)//自己重载的参数的方法
        {
            //判断是否为空
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            //都为空返回全部结果
            //if (string.IsNullOrEmpty(parameters.CompanyName) && string.IsNullOrEmpty(parameters.QueryString))
            //{
            //    return await _context.Companies.ToListAsync();
            //}

            var queryExpression = _context.Companies as IQueryable<Company>;

            //依次添加过滤和查询（此处并没有执行查询）
            if (!string.IsNullOrEmpty(parameters.CompanyName))
            {
                parameters.CompanyName = parameters.CompanyName.Trim();
                queryExpression = queryExpression.Where(x => x.Name == parameters.CompanyName);
            }

            if (!string.IsNullOrEmpty(parameters.QueryString))
            {
                parameters.QueryString = parameters.QueryString.Trim();
                queryExpression = queryExpression
                    .Where(x => x.Name.Contains(parameters.QueryString) || x.Introduction.Contains(parameters.QueryString));
            }

            var mappingDictionary = _propertyMappingService.GetPropertyMapping<CompanyDto, Company>();

            queryExpression = queryExpression.ApplySort(parameters.OrderBy, mappingDictionary);
            //分页
            //queryExpression = queryExpression.Skip((parameters.PageNumber - 1) * parameters.PageSize)
            //    .Take(parameters.PageSize); 

            //执行ToListAsync时自动查询数据库并返回结果
            return await PagedList<Company>.CreateAsync(queryExpression, parameters.PageNumber, parameters.PageSize);
        }

        public void AddCompany(Company company)
        {
            if (company == null)
            {
                throw new ArgumentNullException(nameof(company));
            }

            company.Id = Guid.NewGuid();

            if (company.Employees != null)
            {
                foreach (var employee in company.Employees)
                {
                    employee.Id = Guid.NewGuid();
                }
            }

            _context.Companies.Add(company);
        }

        public void UpdateCompany(Company company)
        {
            //_context.Companies.Update(company); //ef自动追踪
        }

        public void DeleteCompany(Company company)
        {
            if (company == null)
            {
                throw new ArgumentNullException(nameof(company));
            }

            _context.Companies.Remove(company);
        }

        public async Task<bool> CompanyExistsAsync(Guid companyId)
        {
            if (companyId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(companyId));
            }

            return await _context.Companies.AnyAsync(x => x.Id == companyId);
        }

        public async Task<IEnumerable<Employee>> GetEmployeesAsync(Guid companyId, EmployeeDtoParameters parameters)
        {
            if (companyId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(companyId));
            }

            var items = _context.Employees.Where(x => x.CompanyId == companyId);

            //过滤
            if (!string.IsNullOrWhiteSpace(parameters.Gender))
            {
                parameters.Gender = parameters.Gender.Trim();
                var gender = Enum.Parse<Gender>(parameters.Gender);
                items = items.Where(x => x.Gender == gender);
            }

            //搜索
            if (!string.IsNullOrWhiteSpace(parameters.Q))
            {
                var q = parameters.Q.Trim();
                items = items.Where(x => x.FirstName.Contains(q) || x.LastName.Contains(q) || x.EmployeeNo.Contains(q));
            }

            //if (!string.IsNullOrWhiteSpace(parameters.OrderBy))
            //{
            //    if (parameters.OrderBy.ToLowerInvariant() == "name")
            //    {
            //        items = items.OrderBy(x => x.FirstName).ThenBy(x => x.LastName);
            //    }
            //}

            //获取属性的映射关系
            var mappingDictionary = _propertyMappingService.GetPropertyMapping<EmployeeDto, Employee>();

            items = items.ApplySort(parameters.OrderBy, mappingDictionary);

            return await items.ToListAsync();
        }

        public async Task<Employee> GetEmployeeAsync(Guid companyId, Guid employeeId)
        {
            if (companyId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(companyId));
            }

            if (employeeId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(employeeId));
            }

            return await _context.Employees
                .Where(x => x.CompanyId == companyId && x.Id == employeeId)
                .FirstOrDefaultAsync();
        }

        public void AddEmployee(Guid companyId, Employee employee)
        {
            if (companyId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(companyId));
            }

            if (employee == null)
            {
                throw new ArgumentNullException(nameof(employee));
            }

            employee.CompanyId = companyId;
            _context.Employees.Add(employee);
        }

        public void UpdateEmployee(Employee employee)
        {
            // _context.Entry(employee).State = EntityState.Modified;//EF自动追踪实体模型
        }

        public void DeleteEmployee(Employee employee)
        {
            _context.Employees.Remove(employee);
        }

        public async Task<bool> SaveAsync()
        {
            return await _context.SaveChangesAsync() >= 0;
        }
    }
}
