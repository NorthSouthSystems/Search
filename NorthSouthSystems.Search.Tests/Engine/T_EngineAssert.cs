using System.Collections;

internal static class T_EngineAssert
{
    internal static void ExecuteAndAssert(IEnumerable<T_EngineItem> source, Query<int> query, int skip, int take)
    {
        var result = query.Execute(skip, take);

        var amongstPrimaryKeys = new HashSet<int>(query.AmongstPrimaryKeys);

        if (amongstPrimaryKeys.Count > 0)
            source = source.Where(item => amongstPrimaryKeys.Contains(item.Id));

        source = SourceFilter(source, query);

        if (query.SortParameters.Any() || query.SortPrimaryKeyAscending.HasValue)
            source = SourceSort(source, query);

        T_EngineItem[] sourceResults = source.ToArray();

        result.TotalCount.Should().Be(sourceResults.Length);
        result.PrimaryKeys.Should().Equal(sourceResults.Skip(skip).Take(take).Select(item => item.Id).ToArray());

        foreach (var facetParam in query.FacetParametersInternal)
            AssertFacet(sourceResults, facetParam, query.FacetShortCircuitCounting);
    }

    #region Filter

    private static IEnumerable<T_EngineItem> SourceFilter(IEnumerable<T_EngineItem> source, Query<int> query)
    {
        foreach (var param in
                 query.FilterClause == null
                     ? []
                     : query.FilterClause.SubClauses.Cast<IFilterParameter>())
        {
            var closedParam = param;

            source = param.Catalog.Name switch
            {
                "SomeInt" => source.Where(item => SourceFilter(item.SomeInt, closedParam)),
                "SomeDateTime" => source.Where(item => SourceFilter(item.SomeDateTime, closedParam)),
                "SomeString" => source.Where(item => SourceFilter(item.SomeString, closedParam)),
                "SomeTags" => source.Where(item => item.SomeTags.Any(tag => SourceFilter(tag, closedParam))),

                _ => throw new NotImplementedException(param.Catalog.Name)
            };
        }

        return source;
    }

    private static bool SourceFilter(object column, IFilterParameter param) =>
        param.ParameterType switch
        {
            FilterParameterType.Exact => object.Equals(column, param.Exact),
            FilterParameterType.Enumerable => ((IEnumerable)param.Enumerable).Cast<object>().Any(obj => object.Equals(column, obj)),
            FilterParameterType.Range => ((IComparable)column).CompareTo(param.RangeMin) >= 0 && ((IComparable)column).CompareTo(param.RangeMax) <= 0,

            _ => throw new NotImplementedException()
        };

    #endregion

    #region Sort

    private static IOrderedEnumerable<T_EngineItem> SourceSort(IEnumerable<T_EngineItem> source, Query<int> query)
    {
        IOrderedEnumerable<T_EngineItem> sortedSource;

        if (query.SortParameters.Any())
        {
            sortedSource = SourceSort(source, query.SortParameters.First());

            for (int i = 1; i < query.SortParameters.Count(); i++)
                sortedSource = SourceSort(sortedSource, query.SortParameters.ToArray()[i]);

            if (query.SortPrimaryKeyAscending.HasValue)
                return query.SortPrimaryKeyAscending.Value ? sortedSource.ThenBy(item => item.Id) : sortedSource.ThenByDescending(item => item.Id);
            else
                return sortedSource;
        }
        else
            return query.SortPrimaryKeyAscending.Value ? source.OrderBy(item => item.Id) : source.OrderByDescending(item => item.Id);
    }

    private static IOrderedEnumerable<T_EngineItem> SourceSort(IEnumerable<T_EngineItem> source, ISortParameter param) =>
        param.Catalog.Name switch
        {
            "SomeInt" => param.Ascending ? source.OrderBy(item => item.SomeInt) : source.OrderByDescending(item => item.SomeInt),
            "SomeDateTime" => param.Ascending ? source.OrderBy(item => item.SomeDateTime) : source.OrderByDescending(item => item.SomeDateTime),
            "SomeString" => param.Ascending ? source.OrderBy(item => item.SomeString) : source.OrderByDescending(item => item.SomeString),
            "SomeTags" => param.Ascending ? source.OrderBy(item => item.SomeTags.Min()) : source.OrderByDescending(item => item.SomeTags.Max()),

            _ => throw new NotImplementedException(param.Catalog.Name)
        };

    private static IOrderedEnumerable<T_EngineItem> SourceSort(IOrderedEnumerable<T_EngineItem> source, ISortParameter param) =>
        param.Catalog.Name switch
        {
            "SomeInt" => param.Ascending ? source.ThenBy(item => item.SomeInt) : source.ThenByDescending(item => item.SomeInt),
            "SomeDateTime" => param.Ascending ? source.ThenBy(item => item.SomeDateTime) : source.ThenByDescending(item => item.SomeDateTime),
            "SomeString" => param.Ascending ? source.ThenBy(item => item.SomeString) : source.ThenByDescending(item => item.SomeString),
            "SomeTags" => param.Ascending ? source.ThenBy(item => item.SomeTags.Min()) : source.ThenByDescending(item => item.SomeTags.Max()),

            _ => throw new NotImplementedException(param.Catalog.Name)
        };

    #endregion

    #region Facet

    private static void AssertFacet(T_EngineItem[] sourceResults, IFacetParameterInternal param, bool shortCircuitCounting)
    {
        switch (param.Catalog.Name)
        {
            case "SomeInt":
                var sourceSomeIntFacet = sourceResults.GroupBy(item => item.SomeInt)
                    .Select(group => new FacetCategory<int>(group.Key, group.Count()))
                    .OrderBy(facet => facet.Key)
                    .ToArray();

                var paramSomeIntFacet = param.Facet
                    .Categories
                    .Cast<FacetCategory<int>>()
                    .OrderBy(facet => facet.Key)
                    .ToArray();

                sourceSomeIntFacet.AssertFacet(paramSomeIntFacet, shortCircuitCounting);
                break;
            case "SomeDateTime":
                var sourceSomeDateTimeFacet = sourceResults.GroupBy(item => item.SomeDateTime)
                    .Select(group => new FacetCategory<DateTime>(group.Key, group.Count()))
                    .OrderBy(facet => facet.Key)
                    .ToArray();

                var paramSomeDateTimeFacet = param.Facet
                    .Categories
                    .Cast<FacetCategory<DateTime>>()
                    .OrderBy(facet => facet.Key)
                    .ToArray();

                sourceSomeDateTimeFacet.AssertFacet(paramSomeDateTimeFacet, shortCircuitCounting);
                break;
            case "SomeString":
                var sourceSomeStringFacet = sourceResults.GroupBy(item => item.SomeString)
                    .Select(group => new FacetCategory<string>(group.Key, group.Count()))
                    .OrderBy(facet => facet.Key)
                    .ToArray();

                var paramSomeStringFacet = param.Facet
                    .Categories
                    .Cast<FacetCategory<string>>()
                    .OrderBy(facet => facet.Key)
                    .ToArray();

                sourceSomeStringFacet.AssertFacet(paramSomeStringFacet, shortCircuitCounting);
                break;
            case "SomeTags":
                var sourceSomeTagsFacet = sourceResults.SelectMany(item => item.SomeTags)
                    .GroupBy(tag => tag)
                    .Select(group => new FacetCategory<string>(group.Key, group.Count()))
                    .OrderBy(facet => facet.Key)
                    .ToArray();

                var paramSomeTagsFacet = param.Facet
                    .Categories
                    .Cast<FacetCategory<string>>()
                    .OrderBy(facet => facet.Key)
                    .ToArray();

                sourceSomeTagsFacet.AssertFacet(paramSomeTagsFacet, shortCircuitCounting);
                break;
            default:
                throw new NotImplementedException(param.Catalog.Name);
        }
    }

    private static void AssertFacet<T>(this FacetCategory<T>[] categories, FacetCategory<T>[] compare, bool shortCircuitCounting)
        where T : IEquatable<T>, IComparable<T>
    {
        compare.Length.Should().Be(categories.Length);

        for (int i = 0; i < categories.Length; i++)
        {
            compare[i].Key.Should().Be(categories[i].Key);
            compare[i].Count.Should().Be(shortCircuitCounting ? 1 : categories[i].Count);
        }
    }

    #endregion
}
