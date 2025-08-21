using AutoMapper;
using Business;
using Business.Services;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Enums;
using Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : GenericController<Customer, CustomersController>
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(IDataService dataService, ILogger<CustomersController> logger, IMapper mapper) : base(dataService, logger)
        {
            _logger = logger;
            _dataService = dataService;
            _mapper = mapper;
        }

        // GET: api/Customers
        [HttpGet]
        public async Task<IEnumerable<Customer>> GetAll()
        {
            List<Customer> result = await _dataService.Customers.ToListAsync();
            return result;
        }

        // POST: api/Customers/GetDataTable
        [HttpPost("GetDataTable")]
        public async Task<ApiResponse<DataTableDto<CustomerDto>>> GetDataTable([FromBody] DataTableDto<CustomerDto> dataTable)
        {
            Func<IQueryable<Customer>, IOrderedQueryable<Customer>>? orderByQuery = null;
            List<Func<IOrderedQueryable<Customer>, IOrderedQueryable<Customer>>>? thenOrderByQuery = new List<Func<IOrderedQueryable<Customer>, IOrderedQueryable<Customer>>>();
            List<Expression<Func<Customer, bool>>>? filterQuery = new List<Expression<Func<Customer, bool>>>();
            List<Func<IQueryable<Customer>, IIncludableQueryable<Customer, object>>>? includesQuery = new List<Func<IQueryable<Customer>, IIncludableQueryable<Customer, object>>>();

            var query = _dataService.ContactInformations;

            // Handle Sorting of DataTable.
            if (dataTable.MultiSortMeta?.Count() > 0)
            {
                // Create the first OrderBy().
                DataTableSortDto? dataTableSort = dataTable.MultiSortMeta.First();
                if (dataTableSort.Order > 0)
                    query.OrderBy(dataTableSort.Field, OrderDirectionEnum.ASCENDING);
                else if (dataTableSort.Order < 0)
                    query.OrderBy(dataTableSort.Field, OrderDirectionEnum.DESCENDING);

                // Create the rest OrderBy methods as ThenBy() if any.
                foreach (var sortInfo in dataTable.MultiSortMeta.Skip(1))
                {
                    if (dataTableSort.Order > 0)
                        query.ThenBy(sortInfo.Field, OrderDirectionEnum.ASCENDING);
                    else if (dataTableSort.Order < 0)
                        query.ThenBy(sortInfo.Field, OrderDirectionEnum.DESCENDING);
                }
            }


            // Handle Filtering of DataTable.
            if (dataTable.Filters?.FirstName?.Value != null && dataTable.Filters?.FirstName.Value.Length > 0)
                filterQuery.Add(x => x.FirstName.Contains(dataTable.Filters.FirstName.Value));

            if (dataTable.Filters?.LastName?.Value != null && dataTable.Filters?.LastName.Value.Length > 0)
                filterQuery.Add(x => x.LastName.Contains(dataTable.Filters.LastName.Value));


            // Handle Includes.
            includesQuery.Add(x => x.Include(y => y.ContactInformations));


            // Handle pagination.
            query.AddPagging(dataTable.PageCount ?? 10, dataTable.Page ?? 1);


            // Retrieve Data.
            List<ContactInformation> result = await query.ToListAsync();
            List<CustomerDto> customerDto = _mapper.Map<List<CustomerDto>>(result);

            customerDto.SelectMany(x => x.ContactInformations).ToList().ForEach(x => x.Customer = null);

            //TODDO add filter
            int rows = await _dataService.Customers.CountAsync();

            dataTable.Data = customerDto;
            dataTable.PageCount = rows;

            return new ApiResponse<DataTableDto<CustomerDto>>().SetSuccessResponse(dataTable);

        }



        // GET: api/Customers/5
        //[HttpGet("{id}")]
        //public async Task<Customer?> Get(int? id)
        //{
        //    if (id == null)
        //        return null;

        //    Customer? customer = await _dataService.Query.Customers
        //        .Include(x=>x.ContactInformations)
        //        .Include(x=>x.Tickets)
        //        .FirstOrDefaultAsync(m => m.Id == id);

        //    if (customer == null)
        //        return null;

        //    return customer;
        //}

        // POST: api/Customers
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<CustomerDto> Create(CustomerDto customerDto)
        {
            Customer customer = _mapper.Map<Customer>(customerDto);
            customer.CreatedOn = DateTime.Now;

            // Assign id of 0 to each negative contact information.
            customer.ContactInformations
                .Where(x => x.Id < 0)
                .ToList()
                .ForEach(x =>
                {
                    x.Id = 0;
                    x.CreatedOn = DateTime.Now;
                });


            _dataService.Customers.Add(customer);

            customerDto = _mapper.Map<CustomerDto>(customer);

            if (customerDto.ContactInformations != null)
                customerDto.ContactInformations
                    .ToList()
                    .ForEach(x => x.Customer = null);


            return customerDto;
        }


        // PUT: api/Customers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCustomer(int id, Customer customer)
        {
            if (id != customer.Id)
                return BadRequest();


            try
            {
            _dataService.Update(customer);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Customers/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteCustomer(int id)
        //{
        //    var customer = await _dataService.Customers.FindByIdAsync(id);
        //    if (customer == null)
        //    {
        //        return NotFound();
        //    }

        //    _dataService.Customers.Remove(customer);
        //    await _dataService.SaveChangesAsync();

        //    return NoContent();
        //}

        private bool CustomerExists(int id)
        {
            return _dataService.Customers.Any(e => e.Id == id);
        }
    }
}
