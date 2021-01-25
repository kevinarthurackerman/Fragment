interface FragmentConfiguration {
    idAttribute: string;
    onBeforeRequest: (event: OnBeforeRequestEvent) => void;
    onAfterRequest: (event: OnAfterRequestEvent) => void;
    onError(message: string);
}

interface FragmentInfo {
    selector: string;
    position: 'RemoveElement' | 'ReplaceElement' | 'BeforeElement' | 'AfterElement' | 'RemoveContent' | 'ReplaceContent' | 'BeforeContent' | 'AfterContent';
    delay: number;
    contentType: 'text/html' | 'text/javascript';
    content: string;
}

interface Headers {
    // todo: type checking for indexer appears to be broken
    //[headerKey: string]: string;
}

interface MultipartBodyPart {
    body: string;
    headers: Headers;
}

interface OnBeforeRequestEvent {
    triggeringEvent: Event;
}

interface OnAfterRequestEvent {
    triggeringEvent: Event;
    success: boolean;
}

window['fragment'] = {
    idAttribute: 'fragment',
    onBeforeRequest: function () { },
    onAfterRequest: function () { },
    onError: (message: string): void => console.log(message)
} as FragmentConfiguration;

(function (): void {
    const config = (window['fragment'] as FragmentConfiguration);

    function parseMultipartBody(body: string, boundary: string): Array<MultipartBodyPart> {
        return body.split(`--${boundary}`).reduce((parts, part) => {
            if (!!part && part !== '--' && part !== '--\r\n') {
                const endOfHead = part.indexOf('\r\n\r\n');
                const head = part.substr(0, endOfHead).trim();
                const body = part.substr(endOfHead + 4).trim();
                parts.push({
                    body: body,
                    headers: head.split(/\r\n/g).reduce((headers, header) => {
                        const [key, value] = header.split(/:\s+/)
                        headers[key.toLowerCase()] = value
                        return headers as Headers
                    }, {} as Headers)
                })
            }
            return parts as Array<MultipartBodyPart>;
        }, [] as Array<MultipartBodyPart>);
    }

    function createNode(content: string): Node {
        const contentTemplate = document.createElement('template');
        contentTemplate.innerHTML = (content || '').trim();
        return document.importNode(contentTemplate.content, true);
    }

    function insertFragment(fragment: FragmentInfo): void {
        if (fragment.contentType === 'text/html') {
            if (fragment.selector === 'document' || !fragment.selector) {
                document.open();
                document.write(fragment.content);
                document.close();
                return;
            }

            const targetElements = document.querySelectorAll(fragment.selector);
            switch (fragment.position) {
                case 'RemoveElement':
                    targetElements.forEach(e => e.remove());
                    break;
                case 'ReplaceElement':
                case undefined:
                case null:
                    targetElements.forEach(e => e.replaceWith(createNode(fragment.content)));
                    break;
                case 'BeforeElement':
                    targetElements.forEach(e => e.parentNode.insertBefore(createNode(fragment.content), e));
                    break;
                case 'AfterElement':
                    targetElements.forEach(e => e.parentNode.insertBefore(createNode(fragment.content), e.nextSibling));
                    break;
                case 'RemoveContent':
                    targetElements.forEach(e => e.innerHTML = '');
                    break;
                case 'ReplaceContent':
                    targetElements.forEach(e => e.innerHTML = fragment.content);
                    break;
                case 'BeforeContent':
                    targetElements.forEach(e => e.innerHTML = fragment.content + e.innerHTML);
                    break;
                case 'AfterContent':
                    targetElements.forEach(e => e.innerHTML = e.innerHTML + fragment.content);
                    break;
                default:
                    return config.onError(`Position '${fragment.position}' is not a valid position.`);
            }
        } else if (fragment.contentType === 'text/javascript') {
            // todo: implement script insertion behavior
        }
    }

    function scheduleInsertFragment(fragment: FragmentInfo): void {
        if (fragment.delay > 0) {
            window.setTimeout(() => insertFragment(fragment), fragment.delay);
            return;
        }

        insertFragment(fragment);
    }

    function scheduleFragmentInsertion(fragments: Array<FragmentInfo>): void {
        fragments.forEach(f => scheduleInsertFragment(f));
    }

    function fetchContentWithAjax(triggeringEvent: Event, url: string, addHistory: boolean): void {

        config.onBeforeRequest({ triggeringEvent });

        const xhttp = new XMLHttpRequest();
        xhttp.open('GET', url, true);
        xhttp.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
        xhttp.addEventListener('load', () => {
            if (xhttp.status !== 200) {
                document.write(xhttp.responseText);
                config.onAfterRequest({ triggeringEvent, success: false });
                return;
            }
            
            const contentType = xhttp.getResponseHeader('Content-Type');
            const contentTypeSegments = contentType.split(';')
                .map(x => x.split('='))
                .reduce((x, y) => x.concat(y))
                .map(x => x.trim());

            const type = contentTypeSegments[0];

            if (type !== 'multipart/byteranges')
                return config.onError(`Invalid content type '${contentType}'. Expected 'multipart/byteranges'`);

            const boundary = contentTypeSegments[2].slice(1, -1);

            const parts = parseMultipartBody(xhttp.responseText, boundary);

            const fragmentData = parts.map(x => {
                return {
                    selector: x.headers['x-fragment-selector'],
                    position: x.headers['x-fragment-contentposition'],
                    delay: x.headers['x-fragment-delay'] || 0,
                    contentType: x.headers['content-type'],
                    content: x.body
                } as FragmentInfo
            });

            scheduleFragmentInsertion(fragmentData);

            const setUrl = xhttp.getResponseHeader('X-Fragment-Url');
            if (addHistory && !!setUrl) {
                // todo: add a way to add a page title
                window.history.pushState({}, '', setUrl);
            }

            config.onAfterRequest({ triggeringEvent, success: true });
        }, false);
        xhttp.send(null);
    }

    function interceptClickEvent(e: MouseEvent): void {
        const target = (e.target || e.srcElement) as HTMLElement;

        if (!target.hasAttribute(config.idAttribute)) return;

        if (target.tagName !== 'A') return;

        const hrefAttr = target.getAttribute('href');

        if (!hrefAttr) return;
        
        e.preventDefault();

        fetchContentWithAjax(e, hrefAttr, true);
    }
    document.addEventListener('click', interceptClickEvent);

    // fix to "reload" without page load
    window.onpopstate = (e: PopStateEvent) => {
        fetchContentWithAjax(e, window.location.href, false);
        e.preventDefault();
    }
})();