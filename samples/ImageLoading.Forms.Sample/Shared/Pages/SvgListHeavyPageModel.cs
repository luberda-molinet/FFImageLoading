using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Xamarin.Forms;
using Xamvvm;

namespace FFImageLoading.Forms.Sample.Pages
{
    public class SvgListHeavyPageModel : BasePageModel
    {
        public SvgListHeavyPageModel()
        {
            ItemSelectedCommand = new BaseCommand<SelectedItemChangedEventArgs>((arg) =>
            {
                SelectedItem = null;
            });
        }

        public ListHeavyItem SelectedItem { get; set; }

        public ICommand ItemSelectedCommand { get; set; }

        public ObservableCollection<ListHeavyItem> Items { get; set; }

        public void Reload()
        {
            var list = new List<ListHeavyItem>();

            string[] images = {
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/Steps.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/USStates.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/aa.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/accessible.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/acid.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/adobe.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/alphachannel.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/android.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/anim1.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/anim2.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/anim3.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/atom.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/basura.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/beacon.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/betterplace.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/bozo.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/bzr.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/bzrfeed.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/ca.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/car.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/cartman.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/caution.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/cc.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/ch.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/check.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/clippath.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/compass.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/compuserver_msn_Ford_Focus.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/copyleft.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/copyright.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/couch.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/couchdb.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/cygwin.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/debian.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/decimal.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/dh.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/digg.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/displayWebStats.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/dojo.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/dst.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/duck.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/duke.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/dukechain.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/easypeasy.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/eee.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/eff.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/erlang.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/evol.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/facebook.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/faux-art.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/fb.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/feed.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/feedsync.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/fsm.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/gallardo.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/gaussian1.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/gaussian2.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/gaussian3.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/gcheck.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/genshi.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/git.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/gnome2.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/google.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/gpg.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/gump-bench.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/heart.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/heliocentric.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/helloworld.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/hg0.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/http.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/ibm.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/ie-lock.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/ielock.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/ietf.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/image.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/instiki.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/integral.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/irony.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/italian-flag.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/iw.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/jabber.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/jquery.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/json.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/juanmontoya_lingerie.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/legal.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/lineargradient1.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/lineargradient2.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/lineargradient3.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/lineargradient4.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/m.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/mac.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/mail.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/mars.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/masking-path-04-b.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/mememe.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/microformat.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/mono.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/moonlight.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/mozilla.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/msft.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/msie.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/mt.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/mudflap.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/myspace.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/mysvg.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/no.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/ny1.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/obama.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/odf.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/open-clipart.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/openid.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/opensearch.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/openweb.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/opera.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/osa.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/oscon.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/osi.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/padlock.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/patch.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/paths-data-08-t.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/paths-data-09-t.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/pdftk.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/pencil.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/penrose-staircase.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/penrose-tiling.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/php.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/poi.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/pservers-grad-03-b-anim.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/pservers-grad-03-b.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/pull.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/python.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/rack.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/radialgradient1.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/radialgradient2.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/rails.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/raleigh.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/rdf.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/rectangles.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/rest.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/rfeed.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/rg1024_Presentation_with_girl.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/rg1024_Ufo_in_metalic_style.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/rg1024_eggs.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/rg1024_green_grapes.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/rg1024_metal_effect.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/ruby.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/rubyforge.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/scimitar-anim.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/scimitar.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/scion.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/semweb.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/shapes-polygon-01-t.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/shapes-polyline-01-t.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/snake.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/star.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/svg.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/svg2009.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/svg_header-clean.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/tommek_Car.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/twitter.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/ubuntu.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/unicode-han.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/unicode.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/usaf.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/utensils.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/venus.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/video1.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/vmware.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/vnu.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/vote.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/w3c.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/whatwg.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/why.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/wii.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/wikimedia.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/wireless.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/wp.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/wso2.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/x11.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/yadis.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/yahoo.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/yinyang.svg",
                "https://dev.w3.org/SVG/tools/svgweb/samples/svg-files/zillow.svg",
            };

            var howMuch = images.Length;
            var howManyTimes = 10;

            for (int j = 0; j < howManyTimes; j++)
            {
                for (int i = 0; i < howMuch; i++)
                {
                    var item = new ListHeavyItem()
                    {
                        Image1Url = images[i],
                        Image2Url = images[i],
                        Image3Url = images[i],
                        Image4Url = images[i],
                    };

                    list.Add(item);
                }
            }

            Items = new ObservableCollection<ListHeavyItem>(list);
        }


        public class ListHeavyItem : BaseModel
        {
            public string Image1Url { get; set; }

            public string Image2Url { get; set; }

            public string Image3Url { get; set; }

            public string Image4Url { get; set; }
        }
    }
}
