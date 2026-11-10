
using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Services.Interfaces;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/carts")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _service;

        public CartController(ICartService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCart()
        {
            var cart = await _service.CreateCartAsync();

            return CreatedAtAction(
                nameof(GetCart),
                new { cartId = cart.Id },
                cart);
        }

        [HttpGet("{cartId:guid}")]
        public async Task<IActionResult> GetCart(Guid cartId)
        {
            var cart = await _service.GetCartAsync(cartId);

            if (cart == null)
                return NotFound();

            return Ok(cart);
        }

        [HttpPost("{cartId:guid}/items")]
        public async Task<IActionResult> AddItem(
            Guid cartId,
            [FromBody] AddCartItemDto dto)
        {
            try
            {
                var cart = await _service.AddItemAsync(
                    cartId, dto);

                if (cart == null)
                    return NotFound("Cart not found.");

                return Ok(cart);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{cartId:guid}/items/{itemId:int}")]
        public async Task<IActionResult> RemoveItem(
            Guid cartId,
            int itemId)
        {
            var removed = await _service.RemoveItemAsync(
                cartId, itemId);

            if (!removed)
                return NotFound();

            return NoContent();
        }

        [HttpGet("{cartId:guid}/quote")]
        public async Task<IActionResult> GetCartQuote(
            Guid cartId,
            CancellationToken cancellationToken)
        {
            try
            {
                var quote = await _service.GetCartQuoteAsync(
                    cartId,
                    cancellationToken);

                if (quote == null)
                    return NotFound("Cart not found.");

                return Ok(quote);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502,
                    "Unable to retrieve product or supplier data.");
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return StatusCode(504,
                    "A downstream service timed out.");
            }
        }

    }
}
