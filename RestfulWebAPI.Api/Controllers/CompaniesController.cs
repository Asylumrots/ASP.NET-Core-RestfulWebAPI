using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RestfulWebAPI.Api.DtoParameters;
using RestfulWebAPI.Api.Entities;
using RestfulWebAPI.Api.Models;
using RestfulWebAPI.Api.Services;

namespace RestfulWebAPI.Api.Controllers
{
    [ApiController] //要求使用属性路由  自动http400响应 自动推断参数的绑定源 
    //[Route("api/[controller]")]//[controller]会随着类名进行自动更改但是不符合Restful webapi规则
    [Route("api/companies")]
    public class CompaniesController : ControllerBase
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IMapper _mapper;

        public CompaniesController(ICompanyRepository companyRepository, IMapper mapper)
        {
            this._companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
            this._mapper = mapper;
        }

        [HttpGet]
        [HttpHead] //和get差不多，但是不返回body只返回资源上的一些信息
        public async Task<ActionResult<IEnumerable<CompanyDto>>> GetCompanies([FromQuery]CompanyDtoParameters parameters)
        {
            var companies = await _companyRepository.GetCompaniesAsync(parameters);
            //var companyDtos = new List<CompanyDto>();
            ////效率太低 使用mapper进行映射
            //foreach (var item in companies)
            //{
            //    companyDtos.Add(new CompanyDto
            //    {
            //        Id = item.Id,
            //        Name = item.Name
            //    });
            //}
            var companyDtos = _mapper.Map<IEnumerable<CompanyDto>>(companies);

            return Ok(companyDtos);
        }

        [HttpGet("{companyId}", Name = nameof(GetCompany))]
        //[Route("{companyId}")] 
        public async Task<ActionResult<CompanyDto>> GetCompany(Guid companyId)
        {
            ////方法1：通过exist方法进行查找但是可能 并发删除导致数据为空
            //var exist = await _companyRepository.CompanyExistsAsync(companyId);
            //if (!exist)
            //{
            //    return NotFound();
            //}
            var company = await _companyRepository.GetCompanyAsync(companyId);
            if (company == null)
            {
                return NotFound();
            }

            var companyDto = _mapper.Map<CompanyDto>(company);

            return Ok(companyDto);
        }

        [HttpPost]
        public async Task<ActionResult<CompanyDto>> AddCompany(CompanyAddDto company)
        {
            var enetity = _mapper.Map<Company>(company);
            //添加并保存
            _companyRepository.AddCompany(enetity);
            await _companyRepository.SaveAsync();

            //映射为返回模型
            var returnDto = _mapper.Map<CompanyDto>(enetity);
            //1.路由名 2.路由值 3.返回的DTO
            return CreatedAtRoute(nameof(GetCompany), new { companyId = returnDto.Id }, returnDto);
        }

        [HttpOptions]
        public async Task<IActionResult> GetCompaniesOptions()
        {
            Response.Headers.Add("Allow", "GET,POST,OPTIONS");
            return Ok();
        }

        [HttpDelete("{companyId}")]
        public async Task<IActionResult> DeleteCompany(Guid companyId)
        {
            var companyEntity = await _companyRepository.GetCompanyAsync(companyId);

            if (companyEntity == null)
            {
                return NotFound();
            }

            //捷联删除：当删除公司的时候删除公司下的全部员工
            _companyRepository.DeleteCompany(companyEntity);
            await _companyRepository.SaveAsync();

            return NoContent();
        }
    }
}