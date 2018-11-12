using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.FirePad {
	public class EntityManager {
		
  Utils utils = new Utils ();
  Dictionary<string,Dictionary<string,object>> entities_;
  public EntityManager() {
    this.entities_ = null;
			Options option = new Options();
			var attrs = new string[] { "src", "alt", "width", "height", "style", "class"};
			this.register("img", option);   
  }

  public void register (string type, Options options) {
    utils.assert(options.render()!=null, "Entity options should include a 'render' function!");
	utils.assert(options.fromElement()!=null, "Entity options should include a 'fromElement' function!");
    this.entities_[type][type] = options;
  }



  public object tryRenderToElement_ (Entity entity, string renderFn,object entityHandle=null) {
			var type = (string)entity.type; 
				var info = entity.info;
    if (this.entities_[type]!=null && this.entities_[type][renderFn]!=null) {
      //var windowDocument = firepad.document || (window && window.document);
      //var res = this.entities_[type][renderFn](info, entityHandle, windowDocument);
      //if (res) {
      //  if (typeof res === 'string') {
      //    var div = (firepad.document || document).createElement('div');
      //    div.innerHTML = res;
      //    return div.childNodes[0];
      //  } else if (typeof res === 'object') {
      //    firepad.utils.assert(typeof res.nodeType !== 'undefined', 'Error rendering ' + type + ' entity.  render() function' +
      //        ' must return an html string or a DOM element.');
      //    return res;
      //  }
      //}
    }
			return null;
  }


	}

	

}