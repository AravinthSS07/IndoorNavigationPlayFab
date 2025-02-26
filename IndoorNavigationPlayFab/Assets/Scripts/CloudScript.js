handlers.updateMapCatalog = function (args, context) {
    var catalog = args.catalog || [];
    
    // Save the catalog to TitleData for all users to access
    var setTitleDataResult = server.UpdateTitleData({
        Key: "MapCatalog",
        Value: JSON.stringify(catalog)
    });
    
    return { success: true, catalog: catalog };
};