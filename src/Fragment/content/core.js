window['fragment'] = {
    idAttribute: 'fragment',
    onBeforeRequest: function () { },
    onAfterRequest: function () { },
    onError: function (message) { return console.log(message); }
};
(function () {
    var config = window['fragment'];
    function parseMultipartBody(body, boundary) {
        return body.split("--" + boundary).reduce(function (parts, part) {
            if (!!part && part !== '--' && part !== '--\r\n') {
                var endOfHead = part.indexOf('\r\n\r\n');
                var head = part.substr(0, endOfHead).trim();
                var body_1 = part.substr(endOfHead + 4).trim();
                parts.push({
                    body: body_1,
                    headers: head.split(/\r\n/g).reduce(function (headers, header) {
                        var _a = header.split(/:\s+/), key = _a[0], value = _a[1];
                        headers[key.toLowerCase()] = value;
                        return headers;
                    }, {})
                });
            }
            return parts;
        }, []);
    }
    function createNode(content) {
        var contentTemplate = document.createElement('template');
        contentTemplate.innerHTML = (content || '').trim();
        return document.importNode(contentTemplate.content, true);
    }
    function insertFragment(fragment) {
        if (fragment.contentType === 'text/html') {
            if (fragment.selector === 'document' || !fragment.selector) {
                document.open();
                document.write(fragment.content);
                document.close();
                return;
            }
            var targetElements = document.querySelectorAll(fragment.selector);
            switch (fragment.position) {
                case 'RemoveElement':
                    targetElements.forEach(function (e) { return e.remove(); });
                    break;
                case 'ReplaceElement':
                case undefined:
                case null:
                    targetElements.forEach(function (e) { return e.replaceWith(createNode(fragment.content)); });
                    break;
                case 'BeforeElement':
                    targetElements.forEach(function (e) { return e.parentNode.insertBefore(createNode(fragment.content), e); });
                    break;
                case 'AfterElement':
                    targetElements.forEach(function (e) { return e.parentNode.insertBefore(createNode(fragment.content), e.nextSibling); });
                    break;
                case 'RemoveContent':
                    targetElements.forEach(function (e) { return e.innerHTML = ''; });
                    break;
                case 'ReplaceContent':
                    targetElements.forEach(function (e) { return e.innerHTML = fragment.content; });
                    break;
                case 'BeforeContent':
                    targetElements.forEach(function (e) { return e.prepend(createNode(fragment.content)); });
                    break;
                case 'AfterContent':
                    targetElements.forEach(function (e) { return e.append(createNode(fragment.content)); });
                    break;
                default:
                    return config.onError("Position '" + fragment.position + "' is not a valid position.");
            }
        }
        else if (fragment.contentType === 'text/javascript') {
            var scriptTag = document.createElement('script');
            scriptTag.innerHTML = fragment.content;
            document.body.append(scriptTag);
            scriptTag.remove();
        }
    }
    function scheduleInsertFragment(fragment) {
        if (fragment.delay > 0) {
            window.setTimeout(function () { return insertFragment(fragment); }, fragment.delay);
            return;
        }
        insertFragment(fragment);
    }
    function scheduleFragmentInsertion(fragments) {
        fragments.forEach(function (f) { return scheduleInsertFragment(f); });
    }
    function fetchContentWithAjax(triggeringEvent, url, addHistory) {
        config.onBeforeRequest({ triggeringEvent: triggeringEvent });
        var xhttp = new XMLHttpRequest();
        xhttp.open('GET', url, true);
        xhttp.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
        xhttp.addEventListener('load', function () {
            if (xhttp.status !== 200) {
                document.write(xhttp.responseText);
                config.onAfterRequest({ triggeringEvent: triggeringEvent, success: false });
                return;
            }
            var contentType = xhttp.getResponseHeader('Content-Type');
            var contentTypeSegments = contentType.split(';')
                .map(function (x) { return x.split('='); })
                .reduce(function (x, y) { return x.concat(y); })
                .map(function (x) { return x.trim(); });
            var type = contentTypeSegments[0];
            if (type !== 'multipart/byteranges')
                return config.onError("Invalid content type '" + contentType + "'. Expected 'multipart/byteranges'");
            var boundary = contentTypeSegments[2].replace('"', '');
            var parts = parseMultipartBody(xhttp.responseText, boundary);
            var fragmentData = parts.map(function (x) {
                return {
                    selector: x.headers['x-fragment-selector'],
                    position: x.headers['x-fragment-contentposition'],
                    delay: x.headers['x-fragment-delay'] || 0,
                    contentType: x.headers['content-type'],
                    content: x.body
                };
            });
            scheduleFragmentInsertion(fragmentData);
            var setUrl = xhttp.getResponseHeader('X-Fragment-Url');
            if (addHistory && !!setUrl) {
                // todo: add a way to add a page title
                window.history.pushState({}, '', setUrl);
            }
            config.onAfterRequest({ triggeringEvent: triggeringEvent, success: true });
        }, false);
        xhttp.send(null);
    }
    function interceptClickEvent(e) {
        var target = (e.target || e.srcElement);
        if (!target.hasAttribute(config.idAttribute))
            return;
        if (target.tagName !== 'A')
            return;
        var hrefAttr = target.getAttribute('href');
        if (!hrefAttr)
            return;
        e.preventDefault();
        fetchContentWithAjax(e, hrefAttr, true);
    }
    document.addEventListener('click', interceptClickEvent);
    // fix to "reload" without page load
    window.onpopstate = function (e) {
        fetchContentWithAjax(e, window.location.href, false);
        e.preventDefault();
    };
})();
//# sourceMappingURL=core.js.map