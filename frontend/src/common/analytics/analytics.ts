document.addEventListener('DOMContentLoaded', () => { analytics(); }, false);

        declare var window: any;

        export default function analytics(hasToAutoConsent = false) {

            const arraify = function (elements) {
                return [].slice.call(elements);
            };

            /** Creates a cookie. Code based on https://www.quirksmode.org/js/cookies.html */
            const createCookie = function (name, value, days) {
                let expires = '';

                if (days) {
                    const date = new Date();

                    date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
                    expires = '; expires=' + date.toUTCString();
                }

                document.cookie = name + '=' + value + expires + '; path=/';
            };

            /** Reads a cookie. Code based on https://www.quirksmode.org/js/cookies.html */
            const readCookie = function (name) {
                const nameEQ = name + '=';
                const ca = document.cookie.split(';');

                for (let i = 0; i < ca.length; i++) {
                    let c = ca[i];

                    while (c.charAt(0) === ' ') {
                        c = c.substring(1, c.length);
                    }

                    if (c.indexOf(nameEQ) === 0) {
                        return c.substring(nameEQ.length, c.length);
                    }
                }

                return null;
            };

            /** Hides an element from the DOM */
            const hide = function (ele) {
                ele.setAttribute('aria-hidden', 'true');
                ele.classList.add('hidden');
            };

            /** Tracks if the analytics are turned on or not for GDPR compliance */
            let consent = (function () {
                const value = readCookie('jsll');
                // If the cookie has a value, user was previously here so consent is implied because is a returning one
                if (value === 'on') {
                    return true;
                }

                return false;
            }());
            /** The disclaimer notice about cookie tracking */
            const disclaimer = document.querySelector('.disclaimer');

            /** Initializes the analytics library on demand to be compliant */
            const initAdobeAnalytics = function () {
                const config = {
                    syncMuid: window.awa.utils.isValueAssigned(window.awa.cookie.getCookie('jsll')),
                    userConsentCookieName: 'jsll',
                    autoCapture: {
                        lineage: true,
                        scroll: true
                    },
                    coreData: {
                        appId: 'JFK_Files',
                        env: 'prod',
                        market: 'en-us',
                        pageName: 'JFK Files demo',
                        pageType: 'html'
                    },
                    useShortNameForContentBlob: true
                };

                window.awa.init(config);
            };

            if (hasToAutoConsent) {
                consentCookies();
            }

            /** Sets the consent cookie and starts the analytics. */
            function consentCookies() {
                hide(disclaimer);

                if (!window.ga) {
                    let gaID = 'UA-121469252-5';
                    if (window.document.domain == 'https://jfk-demo.azurewebsites.net/#/') {
                        gaID = 'UA-121469252-5';
                    }
                    window.gaTracking(gaID);
                    if (!consent) {
                        window.ga('send', 'pageview');
                    }
                }

                consent = true;
                createCookie('jsll', 'on', 30);
                if (window.awa) {
                    initAdobeAnalytics();
                }
                window.facebookTracking();
                window.twitterTracking();
                window.linkedinTracking();

            }

            const consentCookiesOnClickElement = function () {
                /** User interacting means consent */
                const allLinks = arraify(document.querySelectorAll('a'));
                const buttons = arraify(document.querySelectorAll('button'));

                allLinks.forEach(function (link) {
                    link.addEventListener('click', consentCookies, true);
                });

                buttons.forEach(function (button) {
                    button.addEventListener('click', consentCookies, true);
                });
            };

            consentCookiesOnClickElement();

            if (consent) {
                // We refresh the cookie
                consentCookies();
                return;
            }
            createCookie('jsll', 'off', 30);
        }
