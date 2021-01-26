(function () {
    const test = 'test';

    window.setTimeout(() => console.log(test), 1000);

    console.log('fired');
})();