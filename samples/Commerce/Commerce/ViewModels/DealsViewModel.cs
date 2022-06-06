﻿namespace Commerce.ViewModels;

public record DealsViewModel(IDealService DealService, IProductService ProductService)
{
	public IListFeed<Product> Items => ListFeed.Async(DealService.GetAll);

	public IListFeed<Product> Favorites => ListFeed.Async(ProductService.GetFavorites);

	public async ValueTask RemoveFromFavorite(Product product, CancellationToken ct)
		=> await ProductService.Update(product with { IsFavorite = false }, ct);
}
