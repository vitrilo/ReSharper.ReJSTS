(function(i) {
    i++;
    var callback = function() {
        console.log(i);
    };
    setTimeout(callback);
})(1);