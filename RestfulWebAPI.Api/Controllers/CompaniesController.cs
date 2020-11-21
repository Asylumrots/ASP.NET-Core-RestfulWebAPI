using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using RestfulWebAPI.Api.ActionConstraints;
using RestfulWebAPI.Api.DtoParameters;
using RestfulWebAPI.Api.Entities;
using RestfulWebAPI.Api.Helpers;
using RestfulWebAPI.Api.Models;
using RestfulWebAPI.Api.Services;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace RestfulWebAPI.Api.Controllers
{
    [ApiController] //要求使用属性路由  自动http400响应 自动推断参数的绑定源 
    //[Route("api/[controller]")]//[controller]会随着类名进行自动更改但是不符合Restful webapi规则
    [Route("api/companies")]
    public class CompaniesController : ControllerBase
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IMapper _mapper;
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly IPropertyCheckerService _propertyCheckerService;

        public CompaniesController(
            ICompanyRepository companyRepository,
            IMapper mapper,
            IPropertyMappingService propertyMappingService,
            IPropertyCheckerService propertyCheckerService)
        {
            this._companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
            this._mapper = mapper;
            _propertyMappingService = propertyMappingService ?? throw new ArgumentNullException(nameof(propertyMappingService));
            _propertyCheckerService = propertyCheckerService;
        }

        [HttpGet(Name = nameof(GetCompanies))]
        [HttpHead] //和get差不多，但是不返回body只返回资源上的一些信息
        public async Task<IActionResult> GetCompanies([FromQuery]CompanyDtoParameters parameters)//ActionResult<IEnumerable<CompanyDto>>
        {
            //如果请求的映射不存在则返回badrequest
            if (!_propertyMappingService.ValidMappingExistsFor<CompanyDto, Company>(parameters.OrderBy))
            {
                return BadRequest();
            }
            //如果请求的属性不存在则返回badreuqest
            if (!_propertyCheckerService.TypeHasProperties<CompanyDto>(parameters.Fields))
            {
                return BadRequest();
            }
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
            ////创建前一页和后一页的URL   //10.23更正 在links里已经创建不需要在头上再次显示
            //var previousPageLink = companies.HasPrevious
            //    ? CreateCompaniesResourceUri(parameters, ResourceUriType.PreviousPage) : null;
            //var nextPageLink = companies.HasNext
            //    ? CreateCompaniesResourceUri(parameters, ResourceUriType.NextPage) : null;
            //创建返回的data
            var paginationMetadata = new
            {
                totalCount = companies.TotalCount,
                pageSize = companies.PageSize,
                currentPage = companies.CurrentPage,
                totalPages = companies.TotalPages,
                //previousPageLink,
                //nextPageLink
            };
            //添加响应头  参数1：返回的数据 参数2：使URL不转化符号
            Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));

            var companyDtos = _mapper.Map<IEnumerable<CompanyDto>>(companies);
            var shapedData = companyDtos.ShapeData(parameters.Fields);

            //获取links
            var links = CreateLinksForCompany(parameters, companies.HasPrevious,companies.HasNext);

            //{ value:[XXX],links }
            //添加单个对象的links
            var shapedCompaniesWithLinks = shapedData.Select(c =>
            {
                var companyDic = c as IDictionary<string, object>;
                var companyLinks = CreateLinksForCompany((Guid)companyDic["Id"], null);
                companyDic.Add("links", companyLinks);
                return companyDic;
            });

            //创建返回的对象 
            var dataAndLinksCollections = new
            {
                value = shapedCompaniesWithLinks,
                links
            };

            return Ok(dataAndLinksCollections);
        }

        //produces相当于一个过滤器 允许方法通过符合的媒体类型     //版本控制有的再此加入 "application/vnd.company.company.friendly.v1+json"  "application/vnd.company.company.friendly.v2+json"
        [Produces("application/json",
            "application/vnd.company.hateoas+json",
            "application/vnd.company.company.friendly+json",
            "application/vnd.company.company.friendly.hateoas+json",
            "application/vnd.company.company.full+json",
            "application/vnd.company.company.full.hateoas+json")]
        [HttpGet("{companyId}", Name = nameof(GetCompany))]
        //[Route("{companyId}")] 
        public async Task<ActionResult<CompanyDto>> GetCompany
            (Guid companyId, [FromQuery]string fields,[FromHeader(Name = "Accept")]string mediaType)
        {
            //解析头部 mediaType 如果解析错误则返回badRequest
            if (!MediaTypeHeaderValue.TryParse(mediaType,out MediaTypeHeaderValue parsedMediaType))
            {
                return BadRequest();
            }

            //如果请求的属性不存在则返回badRequest
            if (!_propertyCheckerService.TypeHasProperties<CompanyDto>(fields))
            {
                return BadRequest();
            }
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

            //判断输入的媒体类型来返回指定的数据

            //判断是否已hateoas结尾来判断是否给予links
            var includeLinks = parsedMediaType.SubTypeWithoutSuffix.EndsWith
                ("hateoas", StringComparison.InvariantCultureIgnoreCase);
            IEnumerable<LinkDto> myLinks = new List<LinkDto>();
            if (includeLinks)
            {
                myLinks = CreateLinksForCompany(companyId, fields);
            }

            //如果存在则截取，不存在则原本返回
            var primaryMediaType = includeLinks
                ? parsedMediaType.SubTypeWithoutSuffix
                    .Substring(0, parsedMediaType.SubTypeWithoutSuffix.Length - 8)
                : parsedMediaType.SubTypeWithoutSuffix;

            if (primaryMediaType=="vnd.company.company.full")
            {
                var fullDto = _mapper.Map<CompanyFullDto>(company)
                    .ShapeData(fields) as IDictionary<string,object>;

                if (includeLinks)
                {
                    fullDto.Add("links",myLinks);
                }

                return Ok(fullDto);
            }

            //如果是vnd.company.company.friendly
            var friendly = _mapper.Map<CompanyDto>(company).ShapeData(fields) as IDictionary<string,object>;
            if (includeLinks)
            {
                friendly.Add("links", myLinks);
            }

            return Ok(friendly);

            //var companyDto = _mapper.Map<CompanyDto>(company);

            ////如果mediatype为自定义的规则 则按例返回
            //if (parsedMediaType.MediaType=="application/vnd.company.hateoas+json")
            //{
            //    //加入links
            //    var links = CreateLinksForCompany(companyId, fields);

            //    var linkedDict = companyDto.ShapeData(fields) as IDictionary<string, object>;

            //    linkedDict.Add("links", links);

            //    return Ok(linkedDict);
            //}

            //return Ok(companyDto.ShapeData(fields));

        }

        [HttpPost(Name = nameof(AddCompanyWithBankruptTime))]
        [RequestHeaderMatchesMediaType("Content-Type",
            "application/vnd.company.companyforcreationwithbankrupttime+json")]
        [Consumes("application/vnd.company.companyforcreationwithbankrupttime+json")]
        public async Task<ActionResult<CompanyDto>> AddCompanyWithBankruptTime(CompanyAddWithBankruptTimeDto company)
        {
            var entity = _mapper.Map<Company>(company);
            //添加并保存
            _companyRepository.AddCompany(entity);
            await _companyRepository.SaveAsync();

            //映射为返回模型
            var returnDto = _mapper.Map<CompanyDto>(entity);
            //1.路由名 2.路由值 3.返回的DTO
            return CreatedAtRoute(nameof(GetCompany), new { companyId = returnDto.Id }, returnDto);
        }

        [HttpPost(Name = nameof(AddCompany))]
        [RequestHeaderMatchesMediaType("Content-Type", "application/json",
            "application/vnd.company.companyforcreation+json")]
        [Consumes("application/json", "application/vnd.company.companyforcreation+json")]
        public async Task<ActionResult<CompanyDto>> AddCompany(CompanyAddDto company)
        {
            var entity = _mapper.Map<Company>(company);
            //添加并保存
            _companyRepository.AddCompany(entity);
            await _companyRepository.SaveAsync();

            //映射为返回模型
            var returnDto = _mapper.Map<CompanyDto>(entity);
            //1.路由名 2.路由值 3.返回的DTO
            return CreatedAtRoute(nameof(GetCompany), new { companyId = returnDto.Id }, returnDto);
        }

        [HttpOptions]
        public IActionResult GetCompaniesOptions()
        {
            Response.Headers.Add("Allow", "GET,POST,OPTIONS");
            return Ok();
        }

        [HttpDelete("{companyId}", Name = nameof(DeleteCompany))]
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

        private string CreateCompaniesResourceUri(CompanyDtoParameters parameters, ResourceUriType type)
        {
            //根据枚举来创建Url
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return Url.Link(nameof(GetCompanies), new
                    {
                        fields = parameters.Fields,
                        orderBy = parameters.OrderBy,
                        pageNumber = parameters.PageNumber - 1,
                        pageSize = parameters.PageSize,
                        companyName = parameters.CompanyName,
                        queryString = parameters.QueryString
                    });
                case ResourceUriType.NextPage:
                    return Url.Link(nameof(GetCompanies), new
                    {
                        fields = parameters.Fields,
                        orderBy = parameters.OrderBy,
                        pageNumber = parameters.PageNumber + 1,
                        pageSize = parameters.PageSize,
                        companyName = parameters.CompanyName,
                        queryString = parameters.QueryString
                    });
                default:
                    return Url.Link(nameof(GetCompanies), new
                    {
                        fields = parameters.Fields,
                        orderBy = parameters.OrderBy,
                        pageNumber = parameters.PageNumber,
                        pageSize = parameters.PageSize,
                        companyName = parameters.CompanyName,
                        queryString = parameters.QueryString
                    });
            }
        }

        //自定义一个HATEOAS的链接创建方法
        private IEnumerable<LinkDto> CreateLinksForCompany(Guid companyId, string fields)
        {
            var links = new List<LinkDto>();

            if (fields == null)
            {
                links.Add(new LinkDto(Url.Link(nameof(GetCompany), new { companyId }),
                    "self", "GET"));
            }
            else
            {
                links.Add(new LinkDto(Url.Link(nameof(GetCompany), new { companyId, fields })
                    , "self", "GET"));
            }

            links.Add(new LinkDto(Url.Link(nameof(DeleteCompany), new { companyId }),
                "delete_company", "DELETE"));

            links.Add(new LinkDto(Url.Link(nameof(EmployeesController.AddEmployee), new { companyId }),
                "create_employee_for_company", "POST"));

            links.Add(new LinkDto(Url.Link(nameof(EmployeesController.GetEmployeesByCompanyId), new
            {
                companyId
            }),
                "employee", "GET"));

            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForCompany(CompanyDtoParameters parameters, bool hasPrevious, bool hasNext)
        {
            var links = new List<LinkDto>();

            links.Add(new LinkDto(CreateCompaniesResourceUri(parameters, ResourceUriType.CurrentPage),
                "self", "GET"));

            if (hasPrevious)
            {
                links.Add(new LinkDto(CreateCompaniesResourceUri(parameters, ResourceUriType.PreviousPage),
                    "previous_page", "GET"));
            }
            if (hasNext)
            {
                links.Add(new LinkDto(CreateCompaniesResourceUri(parameters, ResourceUriType.NextPage),
                    "next_page", "GET"));
            }

            return links;
        }
    }
}